using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly ILogger<MessageController> _logger;

    public MessageController(ILogger<MessageController> logger)
    {
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadMessage([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("No file uploaded.");
            return BadRequest("No file uploaded.");
        }

        try
        {
            string content;

            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                content = await reader.ReadToEndAsync();
            }

            if (!IsValidSwiftMessage(content))
            {
                _logger.LogWarning("Invalid SWIFT MT799 message format.");
                return BadRequest("Invalid SWIFT MT799 message format.");
            }

            // BHB - basic header block {1:}
            // AHB - app header block {2:}
            // TXTB - text block {4:}
            // TLRB - trailer block ${5:}

            var messageBlocks = ParseMessage(content);

            var basicHeaderBlock = ParseBasicHeaderBlock(messageBlocks.BHB);
            var appHeaderBlock = ParseApplicationHeaderBlock(messageBlocks.AHB);
            var textBlock = ParseTextBlock(messageBlocks.TXTB);
            var trailerBlock = ParseTrailerBlock(messageBlocks.TLRB);

            var messageEntity = new MessageEntity
            {
                // Basic Header Block
                TypeOfMessage = basicHeaderBlock.TypeOfMessage,
                ServiceLevel = basicHeaderBlock.ServiceLevel,
                BIC = basicHeaderBlock.BIC,
                SessionNumber = basicHeaderBlock.SessionNumber,
                SequenceNumber = basicHeaderBlock.SequenceNumber,

                // Application Header Block
                MessageDirection = appHeaderBlock.MessageDirection,
                MessageType = appHeaderBlock.MessageType,
                ReceiverBIC = appHeaderBlock.ReceiverBIC,
                SenderBIC = appHeaderBlock.SenderBIC,
                AppHeaderSessionNumber = appHeaderBlock.SessionNumber,
                AppHeaderSequenceNumber = appHeaderBlock.SequenceNumber,
                MessagePriority = appHeaderBlock.MessagePriority,

                // Text Block
                TransactionRef = textBlock.Fields.ContainsKey("20") ? textBlock.Fields["20"] : null,
                RelatedRef = textBlock.Fields.ContainsKey("21") ? textBlock.Fields["21"] : null,
                MessageText = textBlock.Fields.ContainsKey("79") ? textBlock.Fields["79"] : null,

                // Trailer Block
                Checksum = trailerBlock.Chk,
                DigitalSignature = trailerBlock.Mac,

                Timestamp = DateTime.UtcNow
            };

            Database.SaveMessage(messageEntity);
            _logger.LogInformation("Message saved successfully.");
            return Ok("Message saved successfully.");


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving message.");
            return StatusCode(500, "Internal server error");
        }
    }

    private bool IsValidSwiftMessage(string content)
    {
        // Basic structure check for MT799 message
        if (!content.StartsWith("{1:") || !content.Contains("{2:") || !content.Contains("{4:") || !content.Contains("{5:"))
        {
            return false;
        }

        // Ensure correct order of blocks
        var bhbIndex = content.IndexOf("{1:");
        var ahbIndex = content.IndexOf("{2:");
        var txtbIndex = content.IndexOf("{4:");
        var tlrbIndex = content.IndexOf("{5:");

        if (bhbIndex == -1 || ahbIndex == -1 || txtbIndex == -1 || tlrbIndex == -1)
        {
            return false;
        }

        if (bhbIndex > ahbIndex || ahbIndex > txtbIndex || txtbIndex > tlrbIndex)
        {
            return false;
        }

        return true;
    }

    private (string BHB, string AHB, string TXTB, string TLRB) ParseMessage(string content)
    {
        string bhb = string.Empty;
        string ahb = string.Empty;
        string txtb = string.Empty;
        string tlrb = string.Empty;

        using (var reader = new StringReader(content))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("{1:"))
                {
                    bhb = line;
                }
                else if (line.StartsWith("{2:"))
                {
                    ahb = line;
                }
                else if (line.StartsWith("{4:"))
                {
                    txtb = line + "\n";
                    while ((line = reader.ReadLine()) != null && !line.StartsWith("{5:"))
                    {
                        txtb += line + "\n";
                    }
                }
                if (line.StartsWith("{5:"))
                {
                    tlrb = line;
                }
            }
        }

        return (bhb, ahb, txtb, tlrb);
    }


    public class BasicHeaderBlock
    {
        public string TypeOfMessage { get; set; }
        public string ServiceLevel { get; set; }
        public string BIC { get; set; }
        public string SessionNumber { get; set; }
        public string SequenceNumber { get; set; }
    }

    private BasicHeaderBlock ParseBasicHeaderBlock(string bhb)
    {
        //{1:F01PRCBBGSFAXXX1111111111}

        var bhbContent = bhb.Substring(3, bhb.Length - 4);
        // "F01PRCBBGSFAXXX1111111111"

        var typeOfMsg = bhbContent.Substring(0, 1);
        // "F"
        var serviceLevel = bhbContent.Substring(1, 2);
        // "01"
        var bankBIC = bhbContent.Substring(3, 12);
        // "PRCBBGSFAXXX"
        var sessionNumber = bhbContent.Substring(15, 4);
        // "1111"
        var sequenceNumber = bhbContent.Substring(19, 6);
        // "111111"

        return new BasicHeaderBlock
        {
            TypeOfMessage = typeOfMsg,
            ServiceLevel = serviceLevel,
            BIC = bankBIC,
            SessionNumber = sessionNumber,
            SequenceNumber = sequenceNumber
        };
    }

    public class ApplicationHeaderBlock
    {
        public string MessageDirection { get; set; }
        public string MessageType { get; set; }
        public string ReceiverBIC { get; set; }
        public string SenderBIC { get; set; }
        public string SessionNumber { get; set; }
        public string SequenceNumber { get; set; }
        public string MessagePriority { get; set; }
    }

    private ApplicationHeaderBlock ParseApplicationHeaderBlock(string ahb)
    {   // {2:O7991111111111ABGRSWACAXXX11111111111111111111N}

        var ahbContent = ahb.Substring(3, ahb.Length - 4);
        // "O7991111111111ABGRSWACAXXX11111111111111111111N"

        var messageDirection = ahbContent.Substring(0, 1);
        // "O"

        var messageType = ahbContent.Substring(1, 3);
        // "799"

        var receiverBIC = ahbContent.Substring(4, 12);
        // "1111111111AB"

        var senderBIC = ahbContent.Substring(16, 11);
        // "GRSWACAXXX"

        var sessionNumber = ahbContent.Substring(27, 4);
        // "1111"

        var sequenceNumber = ahbContent.Substring(31, 6);
        // "111111"

        var messagePriority = ahbContent.Substring(37, 1);
        // "N"

        return new ApplicationHeaderBlock
        {
            MessageDirection = messageDirection,
            MessageType = messageType,
            ReceiverBIC = receiverBIC,
            SenderBIC = senderBIC,
            SessionNumber = sessionNumber,
            SequenceNumber = sequenceNumber,
            MessagePriority = messagePriority
        };
    }

    public class TextBlock
    {
        public Dictionary<string, string> Fields { get; set; }
    }

    private TextBlock ParseTextBlock(string txtb)
    {
        // {4:
        // :20:67 - C111111 - KNTRL
        // :21:30 - 111 - 1111111
        // :79:NA VNIMANIETO NA: OTDEL BANKOVI GARANTSII
        //.
        // OTNOSNO: POTVARJDENIE NA AVTENTICHNOST NA
        // PRIDRUJITELNO PISMO KAM ISKANE ZA
        // PLASHTANE PO BANKOVA GARANCIA
        // .
        // UVAJAEMI KOLEGI,
        // .
        // UVEDOMJAVAME VI, CHE IZPRASHTAME ISKANE ZA
        // PLASHTANE NA STOYNOST BGN 3.100,00, PREDSTAVENO
        // OT NASHIA KLIENT.
        // .
        // S NASTOYASHTOTO POTVARZHDAVAME AVTENTICHNOSTTA NA
        // PODPISITE VARHU PISMOTO NI, I CHE TEZI LICA SA
        // UPALNOMOSHTENI DA PODPISVAT TAKAV DOKUMENT OT
        // IMETO NA BANKATA AD.
        // .
        // POZDRAVI,
        // TARGOVSKO FINANSIRANE
        // -}

        var txtbContent = txtb.Substring(3, txtb.Length - 5).Trim();

        var fields = new Dictionary<string, string>();

        var lines = txtbContent.Split(new[] { '\n' }, StringSplitOptions.None);

        var currentField = string.Empty;

        foreach (var line in lines)
        {
            if (line.StartsWith(":20:") || line.StartsWith(":21:") || line.StartsWith(":79:"))
            {
                currentField = line.Substring(1, 2);
                fields[currentField] = line.Substring(4).Trim();
            }
            else if (!string.IsNullOrWhiteSpace(currentField))
            {
                fields[currentField] += "\n" + line.Trim();
            }
        }

        return new TextBlock
        {
            Fields = fields
        };
    }

    public class TrailerBlock
    {
        public string Mac { get; set; }
        public string Chk { get; set; }
    }

    private TrailerBlock ParseTrailerBlock(string tlrb)
    {

        var mac = string.Empty;
        var chk = string.Empty;

        var tlrbContent = tlrb.Substring(3, tlrb.Length - 4).Trim();

        var macIndex = tlrbContent.IndexOf("{MAC:");
        if (macIndex != -1)
        {
            var endMacIndex = tlrbContent.IndexOf('}', macIndex);
            mac = tlrbContent.Substring(macIndex + 5, endMacIndex - macIndex - 5);
        }

        var chkIndex = tlrbContent.IndexOf("{CHK:");
        if (chkIndex != -1)
        {
            var endChkIndex = tlrbContent.IndexOf('}', chkIndex);
            chk = tlrbContent.Substring(chkIndex + 5, endChkIndex - chkIndex - 5);
        }

        return new TrailerBlock
        {
            Mac = mac,
            Chk = chk
        };
    }

    public class MessageEntity
    {
        public int Id { get; set; }
        // Basic Header Block 
        public string TypeOfMessage { get; set; }
        public string ServiceLevel { get; set; }
        public string BIC { get; set; }
        public string SessionNumber { get; set; }
        public string SequenceNumber { get; set; }

        // Application Header Block
        public string MessageDirection { get; set; }
        public string MessageType { get; set; }
        public string ReceiverBIC { get; set; }
        public string SenderBIC { get; set; }
        public string AppHeaderSessionNumber { get; set; }
        public string AppHeaderSequenceNumber { get; set; }
        public string MessagePriority { get; set; }

        // Text Block
        public string TransactionRef { get; set; }
        public string RelatedRef { get; set; }
        public string MessageText { get; set; }

        // Trailer Block
        public string Checksum { get; set; }
        public string DigitalSignature { get; set; }

        public DateTime Timestamp { get; set; }
    }

}

