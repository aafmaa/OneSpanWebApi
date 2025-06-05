using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneSpanSign.Sdk;
using OneSpanSign.Sdk.Builder;
using OneSpanWebApi.Data;
using OneSpanWebApi.Models;
using OneSpanWebApi.Data;
using System;

namespace OneSpanWebApi.Services
{
    public class OneSpanService
    {
        private readonly string _baseApiUrl;
        private readonly string _apiUrl;
        private readonly string _docPath;
        private readonly string _apiKey;
        private readonly string _senderEmail;
        private readonly ILogger<OneSpanService> _logger;
        private readonly OssClient _ossClient;
        private readonly DBConnectionFactory _dbConnectionFactory;

        public OneSpanService(IOptions<OneSpanOptions> options, ILogger<OneSpanService> logger, DBConnectionFactory dbConnectionFactory)
        {
            var config = options.Value;
            _baseApiUrl = config.BaseApiUrl;
            _apiUrl = _baseApiUrl + "/api";
            _docPath = config.DocPath;
            _apiKey = config.ApiKey;
            _logger = logger;
            _senderEmail = config.SenderEmail;
            _ossClient = new OssClient(_apiKey, _apiUrl);
            _dbConnectionFactory = dbConnectionFactory;
        }

       
        public string GetSignature(BeneficiaryRequest beneficiaryRequest)
        {
            _logger.LogInformation("Starting GetSignature");
            try
            {
                string path = Path.Combine(_docPath, "Templates\\Beneficiary Designation.pdf");
                
                FileStream fs = File.OpenRead(path);
                FieldBuilder date = FieldBuilder.SignatureDate();
                date.WithPositionExtracted();
                date.WithName("Signer1.Date");

                DocumentPackage superDuperPackage = PackageBuilder
                .NewPackageNamed("Beneficiary Designation Form")
                //.WithEmailMessage("Hello Dear Signer. This is custom email message")
                .WithSenderInfo(SenderInfoBuilder.NewSenderInfo(_senderEmail)) //sender invitation needs to be added thru portal under senders tab. this was returnong error - not sure if we can assign sender dynamically
                .WithSettings(DocumentPackageSettingsBuilder.NewDocumentPackageSettings()
                        //.WithoutWatermark()
                        )
                .WithSigner(SignerBuilder.NewSignerWithEmail(beneficiaryRequest.SignerEmail)
                        .WithFirstName(beneficiaryRequest.SignerFirstName)
                        .WithLastName(beneficiaryRequest.SignerLastName)
                        .ChallengedWithQuestions(
                            ChallengeBuilder.FirstQuestion("What is your date of birth?").Answer(beneficiaryRequest.DateOfBirth)
                                            .SecondQuestion("What are the last 4 digits of your SSN?").Answer(beneficiaryRequest.Last4SSN)
                        )
                      )
                .WithDocument(DocumentBuilder.NewDocumentNamed("Beneficiary Designation")
                        .FromStream(fs, DocumentType.PDF)
                        .EnableExtraction()
                .WithSignature(SignatureBuilder
                        .SignatureFor(beneficiaryRequest.SignerEmail)
                        .WithName("Signer1.Fullname1") //reference: https://community.onespan.com/documentation/onespan-sign/guides/feature-guides/developer/document-extraction
                        .WithPositionExtracted()
                        .WithField(date)
                        //.WithStyle(SignatureStyle.HAND_DRAWN)
                        )
                ).Build();

                //Debug.WriteLine("superDuperPackage: " + superDuperPackage.ToString())
                PackageId packageId = _ossClient.CreatePackageOneStep(superDuperPackage);

                _ossClient.SendPackage(packageId);

                // Save package ID to the database database
                SaveSignaturePackageId(packageId.ToString(), beneficiaryRequest.SignerEmail, beneficiaryRequest.DesignationId);


                return packageId.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSignature");
                throw;
            }
        }

        private void SaveSignaturePackageId(string packageId, string signerEmail, string designationId)
        {
            try 
            { 
                using var connection = _dbConnectionFactory.CreateConnection();
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "OneSpanPackage_Insert";
                command.CommandType = System.Data.CommandType.StoredProcedure;

                var packageIdParameter = command.CreateParameter();
                packageIdParameter.ParameterName = "@PackageId";
                packageIdParameter.Value = packageId;
                command.Parameters.Add(packageIdParameter);

                var designationIdParameter = command.CreateParameter();
                designationIdParameter.ParameterName = "@DesignationId";
                designationIdParameter.Value = int.TryParse(designationId, out var id) ? id : (object)DBNull.Value;
                command.Parameters.Add(designationIdParameter);

                var createdAtParameter = command.CreateParameter();
                createdAtParameter.ParameterName = "@CreatedAt";
                createdAtParameter.Value = DateTime.UtcNow;
                command.Parameters.Add(createdAtParameter);

                var signerEmailParameter = command.CreateParameter();
                signerEmailParameter.ParameterName = "@SignerEmail";
                signerEmailParameter.Value = signerEmail ?? (object)DBNull.Value;
                command.Parameters.Add(signerEmailParameter);

                command.ExecuteNonQuery();

                _logger.LogInformation("Signature package id saved: PackageId={PackageId}, SignerEmail={SignerEmail}, DesignationId={DesignationId}", packageId, signerEmail, designationId);
            
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving signature package: PackageId={PackageId}, SignerEmail={SignerEmail}, DesignationId={DesignationId}", packageId, signerEmail, designationId);
                throw;
            }
        }

        public async Task<string> DownloadSignedDocumentAsync(string packageId, string documentid)
        {
            try
            {
                _logger.LogInformation("Starting DownloadSignedDocumentAsync for PackageId={PackageId}", packageId);

                var documents = _ossClient.PackageService.DownloadZippedDocuments(new PackageId(packageId));

                var document = _ossClient.PackageService.DownloadDocument(new PackageId(packageId), documentid);
                string path = Path.Combine(_docPath, $"Documents\\Completed\\{packageId}_Signed.zip");
                
                await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                await fs.WriteAsync(documents, 0, documents.Length); // Fix: Use WriteAsync to write the byte array to the file stream.

                _logger.LogInformation("Signed document downloaded and saved to {TempPath} for PackageId={PackageId}", path, packageId);
                return path;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading signed document for PackageId={PackageId}", packageId);
                throw;
            }
        }

        public async Task CancelPackageAsync(string designationId)
        {
            try
            {
                _logger.LogInformation($"Canceling package with designation ID: {designationId}");

                //retrieve packageId based on designation Id
                string? packageid = this.GetPackageId(designationId);
                if (!string.IsNullOrEmpty(packageid))
                {
                    PackageId packageId = new PackageId(packageid);
                    await Task.Run(() => _ossClient.PackageService.DeletePackage(packageId));
                }
                else 
                {
                    _logger.LogError($"Error canceling package with designation ID: {designationId} due to package id being returned empty from the database");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error canceling package with designation ID: {designationId}");
                throw;
            }
        }

        public string? GetPackageId(string designationId)
        {
            try
            {
                using var connection = _dbConnectionFactory.CreateConnection();
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "OneSpanPackage_GetPackageId";
                command.CommandType = System.Data.CommandType.StoredProcedure;

                var designationIdParameter = command.CreateParameter();
                designationIdParameter.ParameterName = "@DesignationId";
                designationIdParameter.Value = int.TryParse(designationId, out var id) ? id : (object)DBNull.Value;
                command.Parameters.Add(designationIdParameter);

                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    _logger.LogInformation("Retrieved PackageId={PackageId} for DesignationId={DesignationId}", result.ToString(), designationId);
                    return result.ToString();
                }
                else
                {
                    _logger.LogWarning("No PackageId found for DesignationId={DesignationId}", designationId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PackageId for DesignationId={DesignationId}", designationId);
                throw;
            }
        }


        //public void createPackageFromTemplate()
        //{
        //    OssClient ossClient = new OssClient(_apiKey, _apiUrl);

        //    // Create package from template
        //    //to extract placeholder id (help link: https://community.onespan.com/forum/create-package-using-templateid)
        //    /*
        //        Note that placeholderId is not the "Signer1" as you see in the UI label (Signer1 refers to the role name, however SDK doesn't expose role name). You can share the template ID and I can find the placeholder ID for you. Or if you want to find it yourself, log into your OSS UI portal first, then open a new tab and visit with this API URL:
        //        GET /api/packages/{templateId}
        //        It returns the template JSON and you can find the placeholderID under "roles" array > "id".
        //     */

        //    PackageId templateId = new PackageId("Q94-Du55bnrJj2GhYJ4ZkH8mN3w=");

        //    DocumentBuilder bb = DocumentBuilder.NewDocumentNamed("filledpdf");
        //    bb.FromFile(@"C:/Users/ngorbatovskikh/source/repos/OneSpanApiService/OneSpanApiService/Docs/MemberApplication_27600.pdf");

        //    string stampedPdfFilePath = @"C:/Users/ngorbatovskikh/source/repos/OneSpanApiService/OneSpanApiService/Docs/MemberApplication_27600.pdf"; // Path to your stamped PDF document
        //    Document stampedPdfDocument = DocumentBuilder.NewDocumentNamed("Stamped PDF Document")
        //        .FromFile(stampedPdfFilePath)
        //        .WithSignature(SignatureBuilder.SignatureFor("ngorbatovskikh@aafmaa.com")
        //            .OnPage(0)
        //            .AtPosition(224, 36)) // Specify the signature position on the stamped PDF
        //        .Build();

        //    DocumentPackage templatePackage = PackageBuilder
        //    .NewPackageNamed("Simple Term Package")
        //    .DescribedAs("package description")
        //    .WithSigner(SignerBuilder.NewSignerWithEmail("ngorbatovskikh@aafmaa.com")
        //              .WithFirstName("FName")
        //              .WithLastName("LName")
        //              .Replacing(new Placeholder("386c51f0-980d-4f37-8ab5-854ec0e51b79"))
        //            )
        //    .Build();
        //    PackageId simpTermPackageId = ossClient.CreatePackageFromTemplate(templateId, templatePackage);

        //    ossClient.UploadDocument(stampedPdfDocument, simpTermPackageId);

        //    ossClient.SendPackage(simpTermPackageId);

        //}

        //public void downloadDocument(string packageId)
        //{
        //    //OssClient ossClient = new OssClient(_apiKey, _apiUrl);
        //    PackageId pkgId = new PackageId(packageId);
        //    byte[] content = _ossClient.DownloadZippedDocuments(pkgId);
        //    string fileLocation = @$"{_docPath}\Completed\{pkgId}_{DateTime.Now.ToString("yyyy_MM_dd")}.zip";
        //    //Debug.WriteLine("file save path: " + fileLocation);
        //    File.WriteAllBytes(fileLocation, content);

        //}


    }
}
