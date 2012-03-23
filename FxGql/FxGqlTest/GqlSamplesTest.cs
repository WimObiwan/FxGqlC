using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using FxGqlLib;

namespace FxGqlTest
{
	public class GqlSamplesTest
	{
		//static string samplesPath = Path.Combine (Environment.CurrentDirectory, @"../../SampleFiles");
		int succeeded = 0;
		int failed = 0;
		int unknown = 0;
		public GqlEngine engine = new GqlEngine ();

		public GqlSamplesTest ()
		{
		}
		
		static string[] BATHS = { "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "0A", "0B", "0C", "0D", "0E", "0F", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "1A", "1B", "1C", "1D", "1E", "1F", "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "2A", "2B", "2C", "2D", "2E", "2F", "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "3A", "3B", "3C", "3D", "3E", "3F", "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "4A", "4B", "4C", "4D", "4E", "4F", "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "5A", "5B", "5C", "5D", "5E", "5F", "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "6A", "6B", "6C", "6D", "6E", "6F", "70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "7A", "7B", "7C", "7D", "7E", "7F", "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "8A", "8B", "8C", "8D", "8E", "8F", "90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "9A", "9B", "9C", "9D", "9E", "9F", "A0", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8", "A9", "AA", "AB", "AC", "AD", "AE", "AF", "B0", "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "BA", "BB", "BC", "BD", "BE", "BF", "C0", "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "CA", "CB", "CC", "CD", "CE", "CF", "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "DA", "DB", "DC", "DD", "DE", "DF", "E0", "E1", "E2", "E3", "E4", "E5", "E6", "E7", "E8", "E9", "EA", "EB", "EC", "ED", "EE", "EF", "F0", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "FA", "FB", "FC", "FD", "FE", "FF" };

		static string ByteArrayToHexString (byte[] arrayToConvert, string delimiter)
		{
			int lengthRequired = (arrayToConvert.Length + delimiter.Length) * 2;
			StringBuilder tempstr = new StringBuilder (lengthRequired, lengthRequired);
			foreach (byte currentElem in arrayToConvert) {
				tempstr.Append (BATHS [currentElem]);
				tempstr.Append (delimiter);
			}

			return tempstr.ToString ();
		}
		
		private byte[] CalculateStreamHash (Stream stream)
		{
			HashAlgorithm hasher = new SHA256Managed ();
			byte[] hash = hasher.ComputeHash (stream);
			return hash;
		}
		
		private void CheckStream (Stream stream, string targetHash)
		{
			CheckStream (stream, targetHash, null);
		}

		private void CheckStream (Stream stream, string targetHash1, string targetHash2)
		{
			byte[] hash = CalculateStreamHash (stream);
			string hashString = ByteArrayToHexString (hash, "");
			if (targetHash1 == null) {
				Console.WriteLine ("   Initial hash:");
				Console.WriteLine ("      {0}", hashString);
				unknown++;
			} else {
				if (targetHash1 == hashString || targetHash2 == hashString) {
					Console.WriteLine ("   Test OK");
					succeeded++;
				} else {
					Console.WriteLine ("   Test FAILED");
					Console.WriteLine ("   NewHash");
					Console.WriteLine ("      {0}", hashString);
					Console.WriteLine ("   TargetHash");
					Console.WriteLine ("      {0}", targetHash1);
					if (targetHash2 != null)
						Console.WriteLine ("      {0}", targetHash2);
					failed++;
				}
			}
		}
		
		private void TestFile (string fileName, string targetHash)
		{
			TestFile (fileName, targetHash, null);
		}
		
		private void TestFile (string fileName, string targetHash1, string targetHash2)
		{
			Console.WriteLine ("Testing file '{0}'", fileName);
			//string filePath = Path.Combine (samplesPath, fileName);
			using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read)) {
				CheckStream (stream, targetHash1, targetHash2);
			}
		}

		private void TestGql (string command, string targetHash)
		{
			Console.WriteLine ("Testing GQL '{0}'", command);
			
			if (targetHash == null) {
				engine.OutputStream = Console.Out;
				try {
					engine.Execute (command);
				} catch (ParserException parserException) {
					Console.WriteLine ("Exception catched");
					Console.WriteLine (parserException.ToString ());
				}
			}
			
			using (MemoryStream stream = new MemoryStream()) {
				using (TextWriter outputStream = new StreamWriter(stream)) {
					outputStream.NewLine = "\n"; // Unix style text file
					engine.OutputStream = outputStream;
#if !DEBUG
					try {
#endif
						engine.Execute (command);
#if !DEBUG
					} catch (ParserException parserException) {
						Console.WriteLine ("Exception catched");
						Console.WriteLine (parserException.ToString ());
					}
#endif
				
					outputStream.Flush ();
					
					stream.Seek (0, SeekOrigin.Begin);
					CheckStream (stream, targetHash);				
				}
			}
		}
		
		public void Run ()
		{
			// Select expression without file
			TestGql ("select 17", 
				"54183F4323F377B737433A1E98229EAD0FDC686F93BAB057ECB612DAA94002B5");
			TestGql ("select 22, 28", 
				"FAE0E0FBCC1B5DC06546B59F3498DB60F4C0D0453935645BA9C30EF75D8C54A7");
			TestGql ("select 'test'", 
				"F2CA1BB6C7E907D06DAFE4687E579FCE76B37E4E93B7605022DA52E6CCC26FD2");
			TestGql ("select 'te''st'", 
				"42BFEB188201358A1FCE53D0A72C38D6B614B1839ED272FD5BCC56E988E07F3C");
			TestGql ("select 't''es''t'", 
				"4993EB4CA33D8F511F74B014704AE706D958ADCEE332DF657769C3DAF7D8F54B");
			TestGql ("select 'te''''st'", 
				"D7DA6FE98F53901E63B3A1F8D76D7D078553E9D535FBCCA06983C20FCD79BF46");
			TestGql ("select 17, 'my ''test'' string'", 
				"0FE5F56203BC4BC543402061628C85DF046C637918C3724390F97C3C39159CAD");
			TestGql ("select 17, '<this is a test>'", 
				"A71433033AF787897648946340A9361E32A8098E83F4C11E4E434E8660D01EC8");

			// Select from text-files
			TestFile ("SampleFiles/AirportCodes.csv", 
				"27E41DA43A2A310B44AFE1651931F5843DE7C366FBA07A39A721F3027DB56186",
				"89D93B364228247A25FC5AF13507E303B6DB186A2C271EB2DEC09134138A65F8");
			TestGql ("select * from ['SampleFiles/AirportCodes.csv']",
				"34FDBAA2EB778B55E3174213B9B8282E7F5FA78EF68C22A046572F825F9473F2");
			TestGql ("select * from [SampleFiles/AirportCodes.csv]",
				"34FDBAA2EB778B55E3174213B9B8282E7F5FA78EF68C22A046572F825F9473F2");
			TestGql ("select * from ['SampleFiles/AirportCodes*.csv']",
				"34FDBAA2EB778B55E3174213B9B8282E7F5FA78EF68C22A046572F825F9473F2");
			TestGql ("select * from ['SampleFiles/*.csv'] where $line like '%belgium%'",
				"118A735B8252E05853FD53AB5BF4D2223899144E763EE87880AEA0534F0B3FFB");
			TestGql ("select * from ['SampleFiles/*.csv']",
				"061A433CD5329B48CB1FAC416F4505AFEE5D79632DF2A363991711FB8788D573");
			TestGql ("select * from ['SampleFiles/AirportCodes.csv' -recurse]",
				"34FDBAA2EB778B55E3174213B9B8282E7F5FA78EF68C22A046572F825F9473F2");
			TestGql ("select * from ['SampleFiles/AirportCodes2.csv' -recurse]",
				"34FDBAA2EB778B55E3174213B9B8282E7F5FA78EF68C22A046572F825F9473F2");
			TestGql ("select * from ['SampleFiles/AirportCodes*.csv' -recurse]",
				"593F4746169CEF911AC64760DE052B41C5352124397BC0CDF8B50C692AFBC780");
			
			// Select from zip-files
			TestGql ("select * from ['SampleFiles/AirportCodes.csv.zip']",
				"34FDBAA2EB778B55E3174213B9B8282E7F5FA78EF68C22A046572F825F9473F2");
			TestGql ("select * from ['SampleFiles/AirportCodesTwice.zip']",
			    "593F4746169CEF911AC64760DE052B41C5352124397BC0CDF8B50C692AFBC780");
			TestGql ("select * from ['SampleFiles/Airp?rtCodes.csv.zip']",
			    "34FDBAA2EB778B55E3174213B9B8282E7F5FA78EF68C22A046572F825F9473F2");
			TestGql ("select * from ['SampleFiles/AirportCodes.csv.zip'], ['SampleFiles/SubFolder/AirportCodes2.csv.zip']",
			    "593F4746169CEF911AC64760DE052B41C5352124397BC0CDF8B50C692AFBC780");
			TestGql ("select * from ['SampleFiles/AirportCodes*.csv.zip' -recurse]",
			    "593F4746169CEF911AC64760DE052B41C5352124397BC0CDF8B50C692AFBC780");
			TestGql ("select * from ['SampleFiles/AirportCodes.csv*']",
			    "593F4746169CEF911AC64760DE052B41C5352124397BC0CDF8B50C692AFBC780");
			
			// TOP clause
			TestGql ("select top 15 * from ['SampleFiles/AirportCodes.csv']",
				"A2D73DC9E4603A0BFAE02DF9245C1EECA79D50F5EC5A844F8A1357D26EFB78B9");
			
			// DISTINCT clause
			TestGql ("select * from (select distinct matchregex($line, ', (.*?) (?:- .*)?\"') from ['SampleFiles/AirportCodes.csv']) where contains($line, 'Canada')",
				"68ED9EB7F6C5973ED46F3C15CBC48607501042BF4A85E675D563EF1759FBCF80");
			TestGql ("select distinct matchregex($line, ', (.*?) (?:- .*)?\"') from ['SampleFiles/AirportCodes.csv'] where contains($line, 'Canada')",
				"68ED9EB7F6C5973ED46F3C15CBC48607501042BF4A85E675D563EF1759FBCF80");
			TestGql ("select distinct top 15 matchregex($line, ', (.*?) (?:- .*)?\"') from ['SampleFiles/AirportCodes.csv']",
				"94E49AD9A9C243EE66D101AC953C0BBEBCC58EEC07662A72E8E8F91689EEB4A4");
			
			// Query variables: $filename, $lineno, $line, $totallineno
			TestGql ("select top 15 $totallineno, $line from ['SampleFiles/AirportCodes.csv']",
				"DA2B58CAE0F384F863DDB7D7937DA82E01606FA9F12C9CD6EF7DA441FEF7F9AA");
			TestGql ("select top 15 $filename, $lineno, $line from ['SampleFiles/AirportCodes.csv']",
				"913A745EAEB0F5ADF60EDFB63523DF1F44ED0C9AA32660CC3D40EE6783B1C2FA");

			// Function call:
			//    String: 
			//       left, right, substring, trim, ltrim, rtrim
			//       matchregex, replaceregex, escaperegex
			//       contains
			//    Conversion:
			//       convert
			TestGql ("select ((contains('this is a test string', 'test')))",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select * from ['SampleFiles/AirportCodes.csv'] where not contains($line, 'e')", 
				"0250A23407B660298F16DB946ECF1EA32744B34936E83BE5EACD6F8B966A629D");
			TestGql ("select * from ['SampleFiles/AirportCodes.csv'] where contains($line, 'belgium') or contains($line, 'netherlands')", 
				"BDC46A3E3E1787E570F6D454C9931E68EDD2055B87CA971F71FC80887C0181C8");
			TestGql ("select * from ['SampleFiles/AirportCodes.csv'] where contains($line, 'belgium') and not contains($line, 'service')", 
				"C67C117355EDAA851AC3BBE8AA9C35F648B1C3C5CAE8F74BFC4574E12D53E66A");
			TestGql ("select left('this is a test', 6), right('this is a test', 6), substring('this is a test', 2, 6)",
				"6F4EDEA3F29D410BFC12F89D34C1EC840F3C78C083640FFEF8844B80888806B9");
			TestGql ("select top 15 right(left($line, 10), 5) from ['SampleFiles/AirportCodes.csv']",
				"15052CFF66C8A1556F630F1A5FA2B11553C4E18E71385DE9E9162DCCD69ED76E");
			TestGql ("select top 15 *, matchregex($line, ', (.*) \"') from ['SampleFiles/AirportCodes.csv']",
				"C82300FF46DDFDE379AF89012BD8EA0B885A4A2BA7D49D6BBF59ACB3AF5568E6");
			TestGql ("select top 15 *, matchregex($line, '(, )(.*)( \")', '$2') from ['SampleFiles/AirportCodes.csv']",
				"C82300FF46DDFDE379AF89012BD8EA0B885A4A2BA7D49D6BBF59ACB3AF5568E6");
			TestGql ("select contains('this is a test string', 'test')",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select contains('this is a test string', 'test2')",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 17, '<' + trim('  this is a test  ') + '>'", 
				"A71433033AF787897648946340A9361E32A8098E83F4C11E4E434E8660D01EC8");
			TestGql ("select 17, '<' + ltrim('  this is a test  ') + '>'", 
				"E8C53F04D85ABEC68447347AA114F1E12B8539066FBBBCC46D7CE5BB860FF5B1");
			TestGql ("select 17, '<' + rtrim('  this is a test  ') + '>'", 
				"E33B9EA3F64F04A3B3BEE067D5331C72FC0FD0EE6FF7E3EF7EA3770731CC2A66");
			TestGql ("select replaceregex($line, '(belgi)um', '$1ca') from ['SampleFiles/AirportCodes.csv'] where contains($line, 'belgium')", 
				"2714C51C63344A42BA87B0615E8086DEF90726550A50DA352290D1D052954F3D");

			// Conversion:
			TestGql ("select convert(int, '19')", 
				"A9742EB8EE320E006666AEF25AE9AEED948247F3125C9CAFA7CF97B7E7467DD5");
			TestGql ("select 17 + convert(int, 19)", 
				"A4B2C5DB15348C29451E18B8307E5EF81625EA638E807935F39CEAA8D9AC7758");
			TestGql ("select 17 + convert(string, 19)", 
				"F8486352CBFF39416E12F52C5E5A7D570A4742C11A8E84F51DD935985CD0F3F7");
			
			// Operators & expressions:
			//   Operator precedence http://msdn.microsoft.com/en-us/library/ms190276.aspx
			//     Level 1: '~' (Bitwise NOT)
			//     Level 2: '*' (Multiply), '/' (Division), '%' (Modulo)
			//     Level 3: '+' (Positive), '-' (Negative), 
			//              '+' (Add), '+' (Concatenate), '-' (Subtract), 
			//              '&' (Bitwise AND), '^' (Bitwise Exclusive OR), '|' (Bitwise OR)
			//     Level 4: '=', '>', '<', '>=', '<=', '<>', '!=', '!>', '!<' (Comparison operators)
			//     Level 5: NOT
			//     Level 6: AND
			//     Level 7: ALL, ANY, BETWEEN, IN, LIKE, OR, SOME
			//     [  Level 8: '=' (Assignment)  ]
			//   Boolean
			//     NOT, AND, OR
			//   String
			//     '+', '='
			TestGql ("select * from ['SampleFiles/AirportCodes.csv'] where $line match 'bel[gh]ium'", 
				"61973C8BD569D17891983EA785471E60EEB704E950787427DD88A5CE4B660136");
			TestGql ("select * from ['SampleFiles/AirportCodes.csv'] where $line like '%bel_ium%'", 
				"61973C8BD569D17891983EA785471E60EEB704E950787427DD88A5CE4B660136");
			TestGql ("select 17, '<' + 'this is a test' + '>'", 
				"A71433033AF787897648946340A9361E32A8098E83F4C11E4E434E8660D01EC8");
			TestGql ("select '17 - seventeen - 17'", 
				"6F7DAF17CC9973E69D3501C0D1C3630A546E1C020C792A18FD658857CEC8F6FC");
			TestGql ("select '17 - seventeen - ' + 17", 
				"6F7DAF17CC9973E69D3501C0D1C3630A546E1C020C792A18FD658857CEC8F6FC");
			TestGql ("select 17 + ' - seventeen - 17'", 
				"6F7DAF17CC9973E69D3501C0D1C3630A546E1C020C792A18FD658857CEC8F6FC");
			TestGql ("select '17' = '17'",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select '17' = 17",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 17 = '17'",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 17 = 17",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select '17' = '19'",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select '17' = 19",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 17 = '19'",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 17 = 19",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 17 + 2 = 19",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 17 + 19", 
				"A4B2C5DB15348C29451E18B8307E5EF81625EA638E807935F39CEAA8D9AC7758");
			TestGql ("select 17 + 2 = 19",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 19 = 17 + 2",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 40 - 4", 
				"A4B2C5DB15348C29451E18B8307E5EF81625EA638E807935F39CEAA8D9AC7758");
			TestGql ("select 72 / 2", 
				"A4B2C5DB15348C29451E18B8307E5EF81625EA638E807935F39CEAA8D9AC7758");
			TestGql ("select 73 / 2", 
				"A4B2C5DB15348C29451E18B8307E5EF81625EA638E807935F39CEAA8D9AC7758");
			TestGql ("select 4 * 9", 
				"A4B2C5DB15348C29451E18B8307E5EF81625EA638E807935F39CEAA8D9AC7758");
			TestGql ("select 40 - (+ 4)", 
				"A4B2C5DB15348C29451E18B8307E5EF81625EA638E807935F39CEAA8D9AC7758");
			TestGql ("select 17 - (- 19)", 
				"A4B2C5DB15348C29451E18B8307E5EF81625EA638E807935F39CEAA8D9AC7758");
			TestGql ("select ((37 * 8) + 36) % 37", 
				"A4B2C5DB15348C29451E18B8307E5EF81625EA638E807935F39CEAA8D9AC7758");
			TestGql ("select 1 | 2 | 4, 3 ^ 7, 3 & 6, (~3) & 15",
				"81F11DC8808963D8A816B7A6AEA5479C6E9B48D894B22D6C50A3B58074779CC8");
			TestGql ("select 17 < 19, 18 < 18, 19 < 17",
				"AC478E1A177A3428CD5AD614E592F9AAD5839B4B74558141F76CA527AB806282");
			TestGql ("select 17 <= 19, 18 <= 18, 19 <= 17",
				"1C66F39668FFF404E8C3593737E26493A5D0456617B7A8E4BC07781F553DF2D9");
			TestGql ("select 17 <> 19, 18 <> 18, 19 <> 17",
				"256A6328A5045E2D80DC8656DCCFFCC596D291C3C63530DB8AB09055E994B208");
			TestGql ("select 17 = 19, 18 = 18, 19 = 17",
				"C228F3DBCFF0413A8140F938A2F4990F3FAEC7D12DCF4F28AEFD0A27FE7E4ECF");
			TestGql ("select 17 >= 19, 18 >= 18, 19 >= 17",
				"62DC6547F220D3293036D5304D9DACD88EBD635BA14699BC77AF8CD160196FCE");
			TestGql ("select 17 > 19, 18 > 18, 19 > 17",
				"87AA7A22EA72504D8918A7D6BE0C1399F8B038AAC408F98DA625D2DC9F8B3A90");
			TestGql ("select 17 !< 19, 18 !< 18, 19 !< 17",
				"62DC6547F220D3293036D5304D9DACD88EBD635BA14699BC77AF8CD160196FCE");
			TestGql ("select 17 !> 19, 18 !> 18, 19 !> 17",
				"1C66F39668FFF404E8C3593737E26493A5D0456617B7A8E4BC07781F553DF2D9");
			TestGql ("select 17 != 19, 18 != 18, 19 != 17",
				"256A6328A5045E2D80DC8656DCCFFCC596D291C3C63530DB8AB09055E994B208");
			TestGql ("select 3 - (7+(-9))*(-1)", 
				"4355A46B19D348DC2F57C046F8EF63D4538EBB936000F3C9EE954A27460DD865");
			TestGql ("select 18 between 17 and 19",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 17 between 17 and 19",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 19 between 17 and 19",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 16 between 17 and 19",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 20 between 17 and 19",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 'ccc' between 'bbb' and 'ddd'",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 'bbb' between 'bbb' and 'ddd'",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 'ddd' between 'bbb' and 'ddd'",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 'aaa' between 'bbb' and 'ddd'",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 'eee' between 'bbb' and 'ddd'",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 18 not between 17 and 19",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 20 not between 17 and 19",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 'ccc' not between 'bbb' and 'ddd'",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 'eee' not between 'bbb' and 'ddd'",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 'eee' like '%f%'",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 'eee' like '%e%'",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 'eee' not like '%e%'",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 'eee' not like '%f%'",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 'eee' match '[f]+'",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 'eee' match '[e]+'",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 'eee' not match '[e]+'",
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 'eee' not match '[f]+'",
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 15 in (12, 15, 17)",
			         "A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 14 in (12, 15, 17)",
			         "7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 5 in (select top 10 $lineno from [SampleFiles/AirportCodes.csv])",
			         "A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 12 in (select top 10 $lineno from [SampleFiles/AirportCodes.csv])",
			         "7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 15 not in (12, 15, 17)",
			         "7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 14 not in (12, 15, 17)",
			         "A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 5 not in (select top 10 $lineno from [SampleFiles/AirportCodes.csv])",
			         "7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 12 not in (select top 10 $lineno from [SampleFiles/AirportCodes.csv])",
			         "A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 5 = ANY (select top 10 $lineno from [SampleFiles/AirportCodes.csv])",
			         "A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 12 = ANY (select top 10 $lineno from [SampleFiles/AirportCodes.csv])",
			         "7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 5 = SOME (select top 10 $lineno from [SampleFiles/AirportCodes.csv])",
			         "A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 12 = SOME (select top 10 $lineno from [SampleFiles/AirportCodes.csv])",
			         "7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 5 = ALL (select top 10 5 from [SampleFiles/AirportCodes.csv])",
			         "A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 5 = ALL (select top 10 $lineno from [SampleFiles/AirportCodes.csv])",
			         "7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 5 < ANY (select top 10 $lineno from [SampleFiles/AirportCodes.csv])",
			         "A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 12 < ANY (select top 10 $lineno from [SampleFiles/AirportCodes.csv])",
			         "7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 5 < SOME (select top 10 $lineno from [SampleFiles/AirportCodes.csv])",
			         "A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 12 < SOME (select top 10 $lineno from [SampleFiles/AirportCodes.csv])",
			         "7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select 4 < ALL (select top 10 5 from [SampleFiles/AirportCodes.csv])",
			         "A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select 4 < ALL (select top 10 $lineno from [SampleFiles/AirportCodes.csv])",
			         "7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			TestGql ("select exists (select * from ['SampleFiles/AirportCodes.csv'] where contains($line, 'belgium'))", 
				"A9AC0C3AC83C40E1B4C3416066D63D324EE9F8C144641DFEED72D140B6557245");
			TestGql ("select exists (select * from ['SampleFiles/AirportCodes.csv'] where contains($line, 'torhout'))", 
				"7FC755FADC1B31A6696B8ED57C69D2BFC37F5457735C8FCFAE31FCBD7BBA97D5");
			
			// WHERE clause
			TestGql ("select * from ['SampleFiles/AirportCodes.csv'] where contains($line, 'belgium')", 
				"61973C8BD569D17891983EA785471E60EEB704E950787427DD88A5CE4B660136");
			
			// Subquery
			TestGql ("select * from (select * from ['SampleFiles/AirportCodes.csv'])",
				"34FDBAA2EB778B55E3174213B9B8282E7F5FA78EF68C22A046572F825F9473F2");
			TestGql ("select top 15 right($line, 5) from (select left($line, 10) from ['SampleFiles/AirportCodes.csv'])",
				"15052CFF66C8A1556F630F1A5FA2B11553C4E18E71385DE9E9162DCCD69ED76E");
			
			// Multiple sources in FROM clause
			TestGql ("select * from ['SampleFiles/AirportCodes.csv'], ['SampleFiles/AirportCodes.csv']", 
				"593F4746169CEF911AC64760DE052B41C5352124397BC0CDF8B50C692AFBC780");
			TestGql ("select top 15 * from (select top 10 * from ['SampleFiles/AirportCodes.csv']), ['SampleFiles/AirportCodes.csv']", 
				"534A1476952DCF5B94C440077130533D903AD2198AE0C635755C0191A1F5EDBB");

			// ORDER BY clause
			TestGql ("select distinct matchregex($line, ', (.*?) (?:- .*)?\"') from ['SampleFiles/AirportCodes.csv'] where $line match ', (.*?) (?:- .*)?\"' order by matchregex($line, ', (.*?) (?:- .*)?\"')",
			    "9F7AB835C218FD8C696470805224AEB3570F929AE1179B3D69D50099649BFEBF");
			TestGql ("select * from (select distinct matchregex($line, ', (.*?) (?:- .*)?\"') from ['SampleFiles/AirportCodes.csv'] where $line match ', (.*?) (?:- .*)?\"') order by $line",
			    "9F7AB835C218FD8C696470805224AEB3570F929AE1179B3D69D50099649BFEBF");
			TestGql ("select distinct top 10 matchregex($line, ', (.*?) (?:- .*)?\"') from ['SampleFiles/AirportCodes.csv'] where $line match ', (.*?) (?:- .*)?\"' order by matchregex($line, ', (.*?) (?:- .*)?\"')",
				"9FBCD5AA7183396C32D0398480C376BD5A4CCF627C9B4896BE30AF993F603E1A");
			TestGql ("select top 10 * from (select distinct matchregex($line, ', (.*?) (?:- .*)?\"') from ['SampleFiles/AirportCodes.csv'] where $line match ', (.*?) (?:- .*)?\"') order by $line",
				"9FBCD5AA7183396C32D0398480C376BD5A4CCF627C9B4896BE30AF993F603E1A");
			TestGql ("select top 15 * from (select distinct matchregex($line, ', (.*?) (?:- .*)?\"') from ['SampleFiles/AirportCodes.csv'] where $line match ', (.*?) (?:- .*)?\"') order by $line desc",
				"676A6FAADD62BEE23D765B8D8F84D9A7A1BEDE4444F05096C8E202E23024341D");
			TestGql ("select top 15 matchregex($line, ', (.*?) (?:- .*)?\"'), * from ['SampleFiles/AirportCodes.csv'] where $line match ', (.*?) (?:- .*)?\"' order by matchregex($line, ', (.*?) (?:- .*)?\"'), $line desc",
				"D69FBBED985D587B0A4EA672D8285CFD8C6228EA3C48B01FFE0EEDB53D7C760E");
			TestGql ("select top 15 matchregex($line, ', (.*?) (?:- .*)?\"'), * from ['SampleFiles/AirportCodes.csv'] where $line match ', (.*?) (?:- .*)?\"' order by 1, 2 desc",
				"D69FBBED985D587B0A4EA672D8285CFD8C6228EA3C48B01FFE0EEDB53D7C760E");
			TestGql ("select top 15 matchregex($line, ', (.*?) (?:- .*)?\"'), * from ['SampleFiles/AirportCodes.csv'] where $line match ', (.*?) (?:- .*)?\"' order by 1, $line desc",
				"D69FBBED985D587B0A4EA672D8285CFD8C6228EA3C48B01FFE0EEDB53D7C760E");
			TestGql ("select top 15 matchregex($line, ', (.*?) (?:- .*)?\"'), * from ['SampleFiles/AirportCodes.csv'] where $line match ', (.*?) (?:- .*)?\"' order by matchregex($line, ', (.*?) (?:- .*)?\"'), 2 desc",
				"D69FBBED985D587B0A4EA672D8285CFD8C6228EA3C48B01FFE0EEDB53D7C760E");
			
			//TestFile("zippedfile.zip", "8F7422A7F2189623D9DB98FB6C583F276806891545AF3BD92530B98098AA6C0A");
			//TestGql("select * from [smallfile.log]", null);
			
			// INTO clause
			TestGql ("select * into ['test.txt'] from ['SampleFiles/AirportCodes.csv']",
				"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
			TestFile ("test.txt",
				"34FDBAA2EB778B55E3174213B9B8282E7F5FA78EF68C22A046572F825F9473F2", // unix
				"2E548FF714E3A398E8A86857E1584AC9277269E5B61CD619800CBBE0F141AAE5"); // windows
			TestGql ("select 17, '<this is a test>' into ['test.txt']", 
				"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
			TestFile ("test.txt",
				"A71433033AF787897648946340A9361E32A8098E83F4C11E4E434E8660D01EC8", // unix
				"0B99DAF34499129ACC727D61F3F43A481DA2D4AE50A87DAFFAC799DA1325D46C"); // windows
			TestGql ("select * into [test.txt] from ['SampleFiles/AirportCodes.csv']",
				"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
			TestFile ("test.txt",
				"34FDBAA2EB778B55E3174213B9B8282E7F5FA78EF68C22A046572F825F9473F2", // unix
				"2E548FF714E3A398E8A86857E1584AC9277269E5B61CD619800CBBE0F141AAE5"); // windows
			TestGql ("select 17, '<this is a test>' into [test.txt]", 
				"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
			TestFile ("test.txt",
				"A71433033AF787897648946340A9361E32A8098E83F4C11E4E434E8660D01EC8", // unix
				"0B99DAF34499129ACC727D61F3F43A481DA2D4AE50A87DAFFAC799DA1325D46C"); // windows
			TestGql ("select * into ['test.txt' -lineend=unix] from ['SampleFiles/AirportCodes.csv']",
				"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
			TestFile ("test.txt",
				"34FDBAA2EB778B55E3174213B9B8282E7F5FA78EF68C22A046572F825F9473F2");
			TestGql ("select 17, '<this is a test>' into ['test.txt' -lineend=unix]", 
				"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
			TestFile ("test.txt",
				"A71433033AF787897648946340A9361E32A8098E83F4C11E4E434E8660D01EC8");
			TestGql ("select * into ['test.txt' -lineend=dos] from ['SampleFiles/AirportCodes.csv']",
				"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
			TestFile ("test.txt",
				"2E548FF714E3A398E8A86857E1584AC9277269E5B61CD619800CBBE0F141AAE5");
			TestGql ("select 17, '<this is a test>' into ['test.txt' -lineend=dos]", 
				"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
			TestFile ("test.txt",
				"0B99DAF34499129ACC727D61F3F43A481DA2D4AE50A87DAFFAC799DA1325D46C");
			TestGql ("select * into ['test.zip' -lineend=dos] from ['SampleFiles/AirportCodes.csv']",
				"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
			TestGql ("select * from ['test.zip']",
				"34FDBAA2EB778B55E3174213B9B8282E7F5FA78EF68C22A046572F825F9473F2");
			TestGql ("select * into ['test.txt' -lineend=unix] from ['SampleFiles/AirportCodes.csv']",
				"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
			TestGql ("select * into ['test.txt' -append -lineend=unix] from ['SampleFiles/AirportCodes.csv']",
				"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
			TestFile ("test.txt",
				"593F4746169CEF911AC64760DE052B41C5352124397BC0CDF8B50C692AFBC780");
			//TestGql ("select * into ['test.zip' -lineend=unix] from ['SampleFiles/AirportCodes.csv']",
			//	"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
			//TestGql ("select * into ['test.zip' -append -lineend=unix] from ['SampleFiles/AirportCodes.csv']",
			//	"E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855");
			//TestGql ("select * from ['test.zip']",
			//	"593F4746169CEF911AC64760DE052B41C5352124397BC0CDF8B50C692AFBC780");
			File.Delete ("test.txt");
			File.Delete ("test.zip");
			
			// Query batch
			TestGql ("select * from ['SampleFiles/AirportCodes.csv'] select * from ['SampleFiles/AirportCodes.csv']", 
				"593F4746169CEF911AC64760DE052B41C5352124397BC0CDF8B50C692AFBC780");
			TestGql ("select * from ['SampleFiles/AirportCodes.csv']; select * from ['SampleFiles/AirportCodes.csv']", 
				"593F4746169CEF911AC64760DE052B41C5352124397BC0CDF8B50C692AFBC780");
			
			// Comments
			TestGql ("select top 15 matchregex($line, ', (.*?) (?:- .*)?\"'), *" + Environment.NewLine
						+ "from ['SampleFiles/AirportCodes.csv'] " + Environment.NewLine
						+ "where $line match ', (.*?) (?:- .*)?\"' -- this is a single line comment" + Environment.NewLine
						+ "-- this is another single line comment" + Environment.NewLine
						+ "order by 1, 2 desc",
				"D69FBBED985D587B0A4EA672D8285CFD8C6228EA3C48B01FFE0EEDB53D7C760E");
			TestGql ("select top 15 matchregex($line, ', (.*?) (?:- .*)?\"'), * from ['SampleFiles/AirportCodes.csv'] where $line match ', (.*?) (?:- .*)?\"' order by matchregex($line, ', (.*?) (?:- .*)?\"'), 2 desc",
				"D69FBBED985D587B0A4EA672D8285CFD8C6228EA3C48B01FFE0EEDB53D7C760E");
			TestGql ("select top 15 matchregex($line, ', (.*?) (?:- .*)?\"'), *" + Environment.NewLine
						+ "from ['SampleFiles/AirportCodes.csv'] " + Environment.NewLine
						+ "where $line match ', (.*?) (?:- .*)?\"' -- this is a single line comment" + Environment.NewLine
						+ "-- this is another single line comment" + Environment.NewLine
						+ "order by 1, 2 desc",
				"D69FBBED985D587B0A4EA672D8285CFD8C6228EA3C48B01FFE0EEDB53D7C760E");
			TestGql ("select top 15 matchregex($line, ', (.*?) (?:- .*)?\"'), *" + Environment.NewLine
						+ "from ['SampleFiles/AirportCodes.csv'] " + Environment.NewLine
						+ "where $line match ', (.*?) (?:- .*)?\"' -- this is a single line comment" + Environment.NewLine
						+ "-- this is another single line comment" + Environment.NewLine
						+ "order by 1, 2 desc -- final single line comment",
				"D69FBBED985D587B0A4EA672D8285CFD8C6228EA3C48B01FFE0EEDB53D7C760E");

			//TestGql ("select /*block comment*/ 17", 
			//	"54183F4323F377B737433A1E98229EAD0FDC686F93BAB057ECB612DAA94002B5");
			//TestGql ("select top 15 matchregex($line, ', (.*?) (?:- .*)?\"'), * /* this" + Environment.NewLine
			//			+ "is a multi-line" + Environment.NewLine
			//			+ "comment*/ from ['SampleFiles/AirportCodes.csv'] " + Environment.NewLine
			//			+ "where $line match ', (.*?) (?:- .*)?\"'" + Environment.NewLine
			//			+ "order by 1, 2 desc",
			//	"588182E67471BF2C6EDA2CB5164EFCF1238A8675741CAFC1903515B33E59C08C");
			
			// Columns
			TestGql ("select distinct top 15 [Tournament] from ['SampleFiles/Tennis-ATP-2011.csv' -TitleLine]",
				"E55E5D72E548200133B381F911E02E57CA2032C42394C544492FE569C8DA9646");
			TestGql ("select distinct [Tournament] from ['SampleFiles/Tennis-ATP-2011.csv' -TitleLine]",
				"BD8F1A8E6C382AD16D3DC742E3F455BD35AAC26262250D68AB1669AE480CF7CB");
			TestGql ("select distinct top 15 matchregex($line, '^.*?\t.*?\t(.*?)\t') [FieldA], matchregex($line, '^.*?\t(.*?)\t.*?\t') [FieldB] from ['SampleFiles/Tennis-ATP-2011.csv']",
				"39B397F3BBB3FC582683C41C0D73826995E7BDB6D68B2DC4E4AC7D81E0C5B59F");
			TestGql ("select [FieldA], [FieldB] from (select distinct top 15 matchregex($line, '^.*?\t.*?\t(.*?)\t') [FieldA], matchregex($line, '^.*?\t(.*?)\t.*?\t') [FieldB] from ['SampleFiles/Tennis-ATP-2011.csv'])",
				"39B397F3BBB3FC582683C41C0D73826995E7BDB6D68B2DC4E4AC7D81E0C5B59F");
			TestGql ("select [FieldB], [FieldA] from (select distinct top 15 matchregex($line, '^.*?\t.*?\t(.*?)\t') [FieldA], matchregex($line, '^.*?\t(.*?)\t.*?\t') [FieldB] from ['SampleFiles/Tennis-ATP-2011.csv'])",
				"145DA47A1E9E308A0A501B6A24509A94943CF72F642347331D3E7B900E4740E2");
						
			
			Console.WriteLine ();
			Console.WriteLine ("{0} tests done, {1} succeeded, {2} failed, {3} unknown", succeeded + failed + unknown, succeeded, failed, unknown);
		}
		 
		public void RunDevelop ()
		{
			// TODO: group by
			// TODO: aggregates
			// TODO: default column names
			// TODO: output columns as column headings
			// TODO: create file columns on regular expression
			// 
			// TODO: Documentation for -FirstLine
			// TODO: Documentation for column title
		}
	}
}
