using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneSpanSign.Sdk;
using OneSpanSign.Sdk.Builder;
using OneSpanWebApi.Data;
using OneSpanWebApi.Models;
using OneSpanWebApi.Data;
using System;
using GemBox.Document;
using GemBox.Pdf;
using GemBox.Pdf.Forms;
using Azure.Identity;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection.Metadata;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;


namespace OneSpanWebApi.Services
{
    public class OneSpanService
    {
        private readonly string _baseApiUrl;
        private readonly string _apiUrl;
        private readonly string _docPath;
        private readonly string _apiKey;
        private readonly string _senderEmail;
        private readonly int _docExperationDays;
        private readonly ILogger<OneSpanService> _logger;
        private readonly OssClient _ossClient;
        private readonly DBConnectionFactory _dbConnectionFactory;
        private readonly IasService _iasService;
        private readonly string _GemBoxDocumentLicense;
        private readonly string _GemBoxPdfLicense;

        public OneSpanService(IOptions<OneSpanConfig> options, ILogger<OneSpanService> logger, DBConnectionFactory dbConnectionFactory, IasService iasService)
        {
            var config = options.Value;
            _baseApiUrl = config.BaseApiUrl;
            _apiUrl = _baseApiUrl + "/api";
            _docPath = config.DocPath;
            _apiKey = config.ApiKey;
            _logger = logger;
            _senderEmail = config.SenderEmail;
            _docExperationDays = config.DocExperationDays;
            _ossClient = new OssClient(_apiKey, _apiUrl);
            _dbConnectionFactory = dbConnectionFactory;
            _GemBoxDocumentLicense = config.GemBoxDocumentLicense;
            _GemBoxPdfLicense = config.GemBoxPdfLicense;
            _iasService = iasService;
        }

       
        public string GetDesignationSignature(BeneficiaryRequest beneficiaryRequest)
        {
            _logger.LogInformation("Starting GetDesignationSignature");
            try
            {
                //string path = Path.Combine(_docPath, "Templates", "BeneficiaryDesignation.pdf");

                //FileStream fs = File.OpenRead(path);
                MemoryStream ms = new MemoryStream(FillPdfForm(TemplateName.BeneficiaryDesignation, beneficiaryRequest));

                FieldBuilder date = FieldBuilder.SignatureDate();
                date.WithPositionExtracted();
                date.WithName("Signer1.Date");

                DocumentPackage superDuperPackage = PackageBuilder
                .NewPackageNamed("Beneficiary Designation Form")
                //.WithEmailMessage("Hello Dear Signer. This is custom email message")
                .WithSenderInfo(SenderInfoBuilder.NewSenderInfo(_senderEmail)) //sender invitation needs to be added thru portal under senders tab. this was returnong error - not sure if we can assign sender dynamically
                .WithSettings(DocumentPackageSettingsBuilder.NewDocumentPackageSettings()
                        .WithDefaultTimeBasedExpiry()
                        .WithRemainingDays(_docExperationDays) 
                        )
                .WithSigner(SignerBuilder.NewSignerWithEmail(beneficiaryRequest.SignerEmail)
                        .WithFirstName(beneficiaryRequest.SignerFirstName)
                        .WithLastName(beneficiaryRequest.SignerLastName)
                        .ChallengedWithQuestions(
                            ChallengeBuilder.FirstQuestion("What is your date of birth?").Answer(beneficiaryRequest.SignerDateOfBirth)
                                            .SecondQuestion("What are the last 4 digits of your SSN?").Answer(beneficiaryRequest.SignerLast4SSN)
                        )
                      )
                .WithDocument(DocumentBuilder.NewDocumentNamed("Beneficiary Designation")
                        .FromStream(ms, DocumentType.PDF)
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
                SaveSignaturePackageId(packageId.ToString(), beneficiaryRequest);


                return packageId.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSignature");
                throw;
            }
        }

        private void SaveSignaturePackageId(string packageId, BeneficiaryRequest beneficiaryRequest)
        {
            try 
            {
                _logger.LogInformation($"Saving signature package ID: {packageId}");

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
                designationIdParameter.Value = int.TryParse(beneficiaryRequest.DesignationId, out var id) ? id : (object)DBNull.Value;
                command.Parameters.Add(designationIdParameter);

                var createdAtParameter = command.CreateParameter();
                createdAtParameter.ParameterName = "@CreatedAt";
                createdAtParameter.Value = DateTime.UtcNow;
                command.Parameters.Add(createdAtParameter);

                var signerEmailParameter = command.CreateParameter();
                signerEmailParameter.ParameterName = "@SignerEmail";
                signerEmailParameter.Value = beneficiaryRequest.SignerEmail ?? (object)DBNull.Value;
                command.Parameters.Add(signerEmailParameter);

                var cnParameter = command.CreateParameter();
                cnParameter.ParameterName = "@CN";
                cnParameter.Value = beneficiaryRequest.CN;
                command.Parameters.Add(cnParameter);

                //set default value for Canceled field to 0 (false)
                var canceledParameter = command.CreateParameter();
                canceledParameter.ParameterName = "@Canceled";
                canceledParameter.Value = 0;
                command.Parameters.Add(canceledParameter);

                command.ExecuteNonQuery();

                _logger.LogInformation($"Signature package id saved: PackageId={packageId}, SignerEmail={beneficiaryRequest.SignerEmail}, DesignationId={beneficiaryRequest.DesignationId}, CN={beneficiaryRequest.CN}, Canceled=0");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving signature package: PackageId={packageId}, SignerEmail={beneficiaryRequest.SignerEmail}, DesignationId={beneficiaryRequest.DesignationId}");
                throw;
            }
        }

        public void UpdateSignaturePackageId(string designationId)
        {
            try
            {
                _logger.LogInformation($"Updating signature package ID for DesignationId={designationId}");

                using var connection = _dbConnectionFactory.CreateConnection();
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "OneSpanPackage_UpdateCanceled";
                command.CommandType = System.Data.CommandType.StoredProcedure;

                var designationIdParameter = command.CreateParameter();
                designationIdParameter.ParameterName = "@DesignationId";
                designationIdParameter.Value = int.TryParse(designationId, out var id) ? id : (object)DBNull.Value;
                command.Parameters.Add(designationIdParameter);

                int rowsAffected = command.ExecuteNonQuery();
                _logger.LogInformation($"Updated Canceled field to true for DesignationId={designationId}. Rows affected: {rowsAffected}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating Canceled field for DesignationId={designationId}");
                throw;
            }
        }

        public async Task<string> DownloadSignedDocumentAsync(string packageId, string documentid)
        {
            try
            {
                _logger.LogInformation($"Starting DownloadSignedDocumentAsync for PackageId={packageId}");

                //var documents = _ossClient.PackageService.DownloadZippedDocuments(new PackageId(packageId)); //use this if you want to download all documents in a package as a zip file

                var document = _ossClient.PackageService.DownloadDocument(new PackageId(packageId), documentid);
                string path = Path.Combine(_docPath, $"Documents\\Completed\\{packageId}_Signed.pdf");
                
                await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                await fs.WriteAsync(document, 0, document.Length); // Fix: Use WriteAsync to write the byte array to the file stream.

                _logger.LogInformation("Signed document downloaded and saved to {TempPath} for PackageId={PackageId}", path, packageId);

                //todo: save documents to DAL and Update IAS with Designation Status set to Finalized?
                int designationid = this.GetDesignationId(packageId);

                if (designationid > 0)
                {
                    //call IAS to Update Designation Status to Finalized
                    _logger.LogInformation($"Finalizing designation with ID: {designationid}");

                    var res = _iasService.DesignationUpdate(designationid);

                    if (res != null) {
                        _logger.LogInformation($"Finalize designation IAS Response: {res} for designationid {designationid}");
                    }
                }

                return path;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading signed document for PackageId={packageId}");
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

                    //updated the database, set Canceled field to true
                    this.UpdateSignaturePackageId(designationId);
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
                _logger.LogInformation($"Retrieving PackageId for DesignationId={designationId}");
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
                    _logger.LogInformation($"Retrieved PackageId={result.ToString()} for DesignationId={designationId}");
                    return result.ToString();
                }
                else
                {
                    _logger.LogWarning($"No PackageId found for DesignationId={designationId}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving PackageId for DesignationId={designationId}");
                throw;
            }
        }

        public int GetDesignationId(string packageId)
        {
            try
            {
                _logger.LogInformation($"Retrieving DesignationId for PackageId={packageId}");
                using var connection = _dbConnectionFactory.CreateConnection();
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "OneSpanPackage_GetDesignationId";
                command.CommandType = System.Data.CommandType.StoredProcedure;

                var packageIdParameter = command.CreateParameter();
                packageIdParameter.ParameterName = "@PackageId";
                packageIdParameter.Value = packageId; //int.TryParse(packageId, out var id) ? id : (object)DBNull.Value;
                command.Parameters.Add(packageIdParameter);

                var result = command.ExecuteScalar();

                int designationId = 0;
                int.TryParse(result?.ToString(), out designationId);

                if (result != null && result != DBNull.Value)
                {
                    _logger.LogInformation($"Retrieved DesignationId={designationId.ToString()} for PackageId={packageId}");
                    return designationId;
                }
                else
                {
                    _logger.LogWarning($"No DesignationId found for PackageId={packageId}");
                    return designationId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving DesignationId for PackageId={packageId}");
                throw;
            }
        }

        public Byte[] FillPdfForm(TemplateName templateName, BeneficiaryRequest beneficiaryRequest)
        {
            _logger.LogInformation("Starting FillPdfForm");
            try
            {
                string path = Path.Combine(_docPath, "Templates", $"{templateName.ToString()}.pdf");
                if (!File.Exists(path))
                {
                    _logger.LogError($"PDF template not found at path: {path}");
                    throw new FileNotFoundException("PDF template not found", path);
                }

                GemBox.Pdf.ComponentInfo.SetLicense(_GemBoxPdfLicense); //("AN-2023May26-vQYQH4jEP6VLkQ408dBOHSZIjtopp5b5bOfLs0SuLFNvBsBGWTlK0sEtqvqeB078/k2YA2BLrXZ1oAIAkVR3sFDWFXg==A");
                GemBox.Document.ComponentInfo.SetLicense(_GemBoxDocumentLicense); //("DN-2023May26-ZWweOk8La1x368wzUnsol6R7xpHa7qx7vRB6OgjIYxjC7dfKDC2y9iju20tG7vPnlY6hbtAGm9yUzZluXJ9Sfacq5TA==A");

                byte[] file = File.ReadAllBytes(path);
                using (var document = PdfDocument.Load(new MemoryStream(file)))
                {
                    if (document.Form?.Fields != null)
                    {
                        foreach (var kvp in beneficiaryRequest.PdfFieldValues)
                        {
                            var field = document.Form.Fields.FirstOrDefault(f => f.Name == kvp.Key);
                            if (field != null)
                            {
                                field.Value = kvp.Value;

                                // make some fields read-only if desired
                                //if (IsReadOnlyField(kvp.Key))
                                field.ReadOnly = true;
                            }
                            else
                            {
                                _logger.LogWarning($"PDF field '{kvp.Key}' not found in template.");
                            }
                        }
                    }

                    using (var output = new MemoryStream())
                    {
                        document.Save(output, GemBox.Pdf.SaveOptions.Pdf);

                        return output.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filling PDF form");
                throw;
            }
        }

        /// Check if the field name matches the pattern for read-only fields: P1N, P2Rel, S4B, S5% etc.
        private bool IsReadOnlyField(string fieldName)
        {
            return Regex.IsMatch(fieldName, @"^(P[1-5]|S[1-5])(N|B|Rel|%)$");
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

    public enum TemplateName
    {
        BeneficiaryDesignation,
        MemberApplication,
        SimpleTermPackage
        // Add more as needed
    }
}
