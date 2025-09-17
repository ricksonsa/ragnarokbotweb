using System.Text.RegularExpressions;

namespace ConsoleAppTest
{
    internal class Program
    {

        public static async Task<string> SaveImageStreamAsync(Stream imageStream, string fileName)
        {
            var filePath = Path.Combine(fileName);
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await imageStream.CopyToAsync(fileStream);
            return $"{fileName}";
        }

        public static (DateTime Date, string IpAddress, string SteamId, string PlayerName, string ScumId, bool IsLoggedIn, float X, float Y, float Z) Parse(string line)
        {
            string pattern =
                @"^(?<date>\d{4}\.\d{2}\.\d{2}-\d{2}\.\d{2}\.\d{2}): '\s*(?<ip>\d{1,3}(?:\.\d{1,3}){3})\s+(?<steamId>\d{17}):(?<player>.+)\((?<scumId>\d+)\)'\s+(?<status>logged in|logged out)\s+at:\s+X=(?<x>[-+]?\d*\.?\d+)\s+Y=(?<y>[-+]?\d*\.?\d+)\s+Z=(?<z>[-+]?\d*\.?\d+)$";

            var match = Regex.Match(line, pattern);
            if (!match.Success)
                throw new FormatException("Log line not in expected format");

            var date = DateTime.ParseExact(match.Groups["date"].Value, "yyyy.MM.dd-HH.mm.ss", null);
            var ip = match.Groups["ip"].Value;
            var steamId = match.Groups["steamId"].Value;
            var player = match.Groups["player"].Value.Trim();
            var scumId = match.Groups["scumId"].Value;
            var status = match.Groups["status"].Value == "logged in";
            var x = float.Parse(match.Groups["x"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var y = float.Parse(match.Groups["y"].Value, System.Globalization.CultureInfo.InvariantCulture);
            var z = float.Parse(match.Groups["z"].Value, System.Globalization.CultureInfo.InvariantCulture);

            return (date, ip, steamId, player, scumId, status, x, y, z);
        }



        static async Task Main(string[] args)
        {
            try
            {
                var mapExtractor = new ScumMapExtractor("island.jpg");

                // Coordenada central - centro do mapa (setor B2)
                var centerCoord = ScumCoordinate.FromSectorCenter("B2");


                // Lista de pontos usando diferentes métodos
                //var points = new List<ScumCoordinate>
                //{
                //    // Centros de outros setores
                //    //ScumCoordinate.FromSectorCenter("Z0"),
                //    //ScumCoordinate.FromSectorCenter("A1"),
                //    //ScumCoordinate.FromSectorCenter("C3"),
                //    //ScumCoordinate.FromSectorCenter("D4"),

                //    // Coordenadas com offset do centro de um setor
                //    //ScumCoordinate.FromSectorWithOffset("B2", 50000, -30000), // 50m leste, 30m sul do centro B2

                //    // Coordenada manual (exemplo: ponto personalizado)
                //    new ScumCoordinate(137592.1875f, -31476.5983f, Color.Blue),
                //    new ScumCoordinate(285811.59375f, -21049.46875f, Color.Red),
                //};
                // {X=-250758.141 Y=-37223.129 Z=35788.141|P=329.433746 Y=234.574814 R=0.000000}
                var points = new List<ScumCoordinate>
                {
                    new ScumCoordinate(-250758.141f, -37223.129f),
                    new ScumCoordinate(-250758.141f, -37223.129f),
                    new ScumCoordinate(-250758.141f, -37223.129f),
                    new ScumCoordinate(-250758.141f, -37223.129f)
                };
                var mid = ScumCoordinate.MidPoint((285811.59375f, -21049.46875f), (137592.1875f, -31476.5983f));
                var stream = await mapExtractor.ExtractMapWithPointsWithWatermark(
                    mid,
                    points,
                    extractSize: 256
                );
                await SaveImageStreamAsync(stream, "output.png");
                // {X=22759.377 Y=-676618.812 Z=340.134|P=348.058350 Y=21.935032 R=0.000000}
                var result = await mapExtractor.ExtractCompleteSector("B2", points);

                await SaveImageStreamAsync(result, "sector.png");

                // Extrai o setor B2 completo
                //mapExtractor.ExtractCompleteSector("B2", [ScumCoordinate.FromSectorCenter("B2")], "sector_B2.png", showLabel: true);

                // Mostra informações dos pontos
                Console.WriteLine("\nPontos no mapa:");
                foreach (var point in points)
                {
                    Console.WriteLine($"Ponto: {point}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
            }
        }
    }
}
