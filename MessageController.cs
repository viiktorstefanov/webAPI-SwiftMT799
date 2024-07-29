using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text.Json;


[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly ILogger<MessageController> _logger;

    public MessageController(ILogger<MessageController> logger)
    {
        _logger = logger;
    }

    [HttpGet("messages")]
    public IActionResult GetAllMessages()
    {
        try
        {
            var messages = Database.GetAllMessages();

            if (messages == null || messages.Count == 0)
            {
                _logger.LogInformation("No messages found.");
                return NotFound("No messages found.");
            }

            _logger.LogInformation("All messages have been sent successfully.");
            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages.");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadMessage(IFormFile file)
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

            _logger.LogInformation("File content read successfully.");

            if (!IsValidMT799SwiftMessage(content))
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

            if (basicHeaderBlock == null)
            {
                _logger.LogWarning("Basic Header Block is missing or invalid.");
                return BadRequest("Basic Header Block is missing or invalid.");
            }

            if (appHeaderBlock == null)
            {
                _logger.LogWarning("Application Header Block is missing or invalid.");
                return BadRequest("Application Header Block is missing or invalid.");
            }

            if (textBlock == null)
            {
                _logger.LogWarning("Text Block is missing or invalid.");
                return BadRequest("Text Block is missing or invalid.");
            }

            if (trailerBlock == null)
            {
                _logger.LogWarning("Trailer Block is missing or invalid.");
                return BadRequest("Trailer Block Block is missing or invalid.");
            }

            var transactionRef = textBlock?.Fields.ContainsKey("20") == true ? textBlock.Fields["20"] : null;
            var relatedRef = textBlock?.Fields.ContainsKey("21") == true ? textBlock.Fields["21"] : null;
            var messageText = textBlock?.Fields.ContainsKey("79") == true ? textBlock.Fields["79"] : null;

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

                TransactionRef = transactionRef,
                RelatedRef = relatedRef,
                MessageText = messageText,

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

    private bool IsValidMT799SwiftMessage(string content)
    {
        string swiftMessagePattern = @"\{1:.*?\{2:.*?\{4:.*?\{5:.*?\}";

        Regex swiftMessageRegex = new Regex(swiftMessagePattern, RegexOptions.Compiled | RegexOptions.Singleline);

        return swiftMessageRegex.IsMatch(content);
    }

    private (string BHB, string AHB, string TXTB, string TLRB) ParseMessage(string content)
    {
        string bhb = string.Empty;
        string ahb = string.Empty;
        string txtb = string.Empty;
        string tlrb = string.Empty;

        string bhbPattern = @"(\{1:.*?\})(?=\{2:|\Z)";
        string ahbPattern = @"(\{2:.*?\})(?=\{4:|\Z)";
        string txtbPattern = @"(\{4:.*?)(?=\{5:|\Z)";
        string tlrbPattern = @"(\{5:.*?\})(?=\Z)";


        var bhbMatch = Regex.Match(content, bhbPattern, RegexOptions.Singleline);
        var ahbMatch = Regex.Match(content, ahbPattern, RegexOptions.Singleline);
        var txtbMatch = Regex.Match(content, txtbPattern, RegexOptions.Singleline);
        var tlrbMatch = Regex.Match(content, tlrbPattern, RegexOptions.Singleline);

        if (bhbMatch.Success)
        {
            bhb = bhbMatch.Value;
        }

        if (ahbMatch.Success)
        {
            ahb = ahbMatch.Value;
        }

        if (txtbMatch.Success)
        {
            txtb = txtbMatch.Value;
        }

        if (tlrbMatch.Success)
        {
            tlrb = tlrbMatch.Value;
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
        public int messageId { get; set; }
        // Basic Header Block
        public string? TypeOfMessage { get; set; }
        public string? ServiceLevel { get; set; }
        public string? BIC { get; set; }
        public string? SessionNumber { get; set; }
        public string? SequenceNumber { get; set; }

        // Application Header Block
        public string? MessageDirection { get; set; }
        public string? MessageType { get; set; }
        public string? ReceiverBIC { get; set; }
        public string? SenderBIC { get; set; }
        public string? AppHeaderSessionNumber { get; set; }
        public string? AppHeaderSequenceNumber { get; set; }
        public string? MessagePriority { get; set; }

        // Text Block
        public string? TransactionRef { get; set; }
        public string? RelatedRef { get; set; }
        public string? MessageText { get; set; }

        // Trailer Block
        public string? Checksum { get; set; }
        public string? DigitalSignature { get; set; }

        public DateTime Timestamp { get; set; }
    }


}

