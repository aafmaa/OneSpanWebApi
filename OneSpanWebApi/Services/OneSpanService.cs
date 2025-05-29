using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneSpanSign.Sdk;
using OneSpanSign.Sdk.Builder;

namespace OneSpanWebApi.Services
{
    public class OneSpanService
    {
        private readonly string _baseApiUrl;
        private readonly string _apiUrl;
        private readonly string _docPath;
        private readonly string _apiKey;
        private readonly ILogger<OneSpanService> _logger;

        public OneSpanService(IOptions<OneSpanOptions> options, ILogger<OneSpanService> logger)
        {
            var config = options.Value;
            _baseApiUrl = config.BaseApiUrl;
            _apiUrl = _baseApiUrl + "/api";
            _docPath = config.DocPath;
            _apiKey = config.ApiKey;
            _logger = logger;
        }

        //private static String BASE_API_URL = "https://sandbox.esignlive.com";
        //private static String API_URL = BASE_API_URL + "/api";
        //private static string DOC_PATH = @"C:\Users\ngorbatovskikh\source\repos\OneSpanApiService\OneSpanApiService\Docs";
        //private static String API_KEY = "SFZtWUpiS1h3SGNIOmw2azJrMllyOGFJWg==";

        public string GetSignature()
        {
            _logger.LogInformation("Starting GetSignature");
            try
            {
                OssClient ossClient = new OssClient(_apiKey, _apiUrl);
            string path = _docPath + @"\MemberApplication_27600.pdf";
            FileStream fs = File.OpenRead(path);
            FieldBuilder date = FieldBuilder.SignatureDate();
            date.WithPositionExtracted();
            date.WithName("Signer1.Date");

            FieldBuilder date2 = FieldBuilder.SignatureDate();
            date2.WithPositionExtracted();
            date2.WithName("Signer2.Date2");

            DocumentPackage superDuperPackage = PackageBuilder
            .NewPackageNamed("Test Package .NET")
            .WithEmailMessage("Hello Dear Signer. This is custom email message")
            .WithSenderInfo(SenderInfoBuilder.NewSenderInfo("ngorbatovskikh@metrostar.com")) //sender invitation needs to be added thru portal under senders tab. this was returnong error - not sure if we can assign sender dynamically
            .WithSettings(DocumentPackageSettingsBuilder.NewDocumentPackageSettings()
                    //.WithoutWatermark()
                    )
            .WithSigner(SignerBuilder.NewSignerWithEmail("ngorbatovskikh@aafmaa.com")
                    .WithFirstName("Ann")
                    .WithLastName("Smith")
                  )
            .WithDocument(DocumentBuilder.NewDocumentNamed("SimpleTerm")
                    .FromStream(fs, DocumentType.PDF)
                    .EnableExtraction()
            .WithSignature(SignatureBuilder
                    .SignatureFor("ngorbatovskikh@aafmaa.com")
                    .WithName("Signer1.Fullname1") //reference: https://community.onespan.com/documentation/onespan-sign/guides/feature-guides/developer/document-extraction
                    .WithPositionExtracted()
                    .WithField(date)
                    )
             .WithSignature(SignatureBuilder
                    .SignatureFor("ngorbatovskikh@aafmaa.com")
                    .WithName("Signer2.Fullname2") //reference: https://community.onespan.com/documentation/onespan-sign/guides/feature-guides/developer/document-extraction
                    .WithPositionExtracted()
                    .WithField(date2)
                    .WithStyle(SignatureStyle.HAND_DRAWN)
                    )

            //.OnPage(1)
            //.AtPosition(36 * 1.3, 1123-224*1.4))
            //.WithSignature(SignatureBuilder
            //        .SignatureFor("ngorbatovskikh@metrostar.com")
            //        .OnPage(0)
            //        .AtPosition(550, 165))

            ).Build();
            //Debug.WriteLine("superDuperPackage: " + superDuperPackage.ToString())
            PackageId packageId = ossClient.CreatePackageOneStep(superDuperPackage);
            ossClient.SendPackage(packageId);

            return packageId.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSignature");
                throw;
            }
        }

        public void createPackageFromTemplate()
        {
            OssClient ossClient = new OssClient(_apiKey, _apiUrl);

            // Create package from template
            //to extract placeholder id (help link: https://community.onespan.com/forum/create-package-using-templateid)
            /*
                Note that placeholderId is not the "Signer1" as you see in the UI label (Signer1 refers to the role name, however SDK doesn't expose role name). You can share the template ID and I can find the placeholder ID for you. Or if you want to find it yourself, log into your OSS UI portal first, then open a new tab and visit with this API URL:
                GET /api/packages/{templateId}
                It returns the template JSON and you can find the placeholderID under "roles" array > "id".
             */

            PackageId templateId = new PackageId("Q94-Du55bnrJj2GhYJ4ZkH8mN3w=");

            DocumentBuilder bb = DocumentBuilder.NewDocumentNamed("filledpdf");
            bb.FromFile(@"C:/Users/ngorbatovskikh/source/repos/OneSpanApiService/OneSpanApiService/Docs/MemberApplication_27600.pdf");

            string stampedPdfFilePath = @"C:/Users/ngorbatovskikh/source/repos/OneSpanApiService/OneSpanApiService/Docs/MemberApplication_27600.pdf"; // Path to your stamped PDF document
            Document stampedPdfDocument = DocumentBuilder.NewDocumentNamed("Stamped PDF Document")
                .FromFile(stampedPdfFilePath)
                .WithSignature(SignatureBuilder.SignatureFor("ngorbatovskikh@aafmaa.com")
                    .OnPage(0)
                    .AtPosition(224, 36)) // Specify the signature position on the stamped PDF
                .Build();

            DocumentPackage templatePackage = PackageBuilder
            .NewPackageNamed("Simple Term Package")
            .DescribedAs("package description")
            .WithSigner(SignerBuilder.NewSignerWithEmail("ngorbatovskikh@aafmaa.com")
                      .WithFirstName("FName")
                      .WithLastName("LName")
                      .Replacing(new Placeholder("386c51f0-980d-4f37-8ab5-854ec0e51b79"))
                    )
            .Build();
            PackageId simpTermPackageId = ossClient.CreatePackageFromTemplate(templateId, templatePackage);

            ossClient.UploadDocument(stampedPdfDocument, simpTermPackageId);

            ossClient.SendPackage(simpTermPackageId);

        }

        public void downloadDocument(string packageId)
        {
            OssClient ossClient = new OssClient(_apiKey, _apiUrl);
            PackageId pkgId = new PackageId(packageId);
            byte[] content = ossClient.DownloadZippedDocuments(pkgId);
            string fileLocation = @$"{_docPath}\SignedDocs\{pkgId}_{DateTime.Now.ToString("yyyy_MM_dd")}.zip";
            //Debug.WriteLine("file save path: " + fileLocation);
            File.WriteAllBytes(fileLocation, content);

        }
    }
}
