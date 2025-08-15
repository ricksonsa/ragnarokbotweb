using Newtonsoft.Json;
using Shared.Models;
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


        public static List<ScumPlayer> Parse(string data)
        {
            var players = new List<ScumPlayer>();
            string[] values = data.Trim().Split(new string[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                foreach (var value in values)
                {
                    var pattern = @"^\d+\.\s*(?<name>.+)\r?\nSteam: (?<steamName>.+) \((?<steamId>\d+)\)\r?\nFame: (?<fame>\d+)\s*\r?\nAccount balance: (?<accountBalance>\d+)\r?\nGold balance: (?<goldBalance>\d+)\r?\nLocation: X=(?<x>-?\d+\.?\d*) Y=(?<y>-?\d+\.?\d*) Z=(?<z>-?\d+\.?\d*)";

                    var match = Regex.Match(value.TrimStart(), pattern, RegexOptions.Multiline);
                    if (match.Success)
                    {
                        players.Add(new ScumPlayer
                        {
                            Name = match.Groups["name"].Value,
                            SteamName = match.Groups["steamName"].Value,
                            SteamID = match.Groups["steamId"].Value,
                            Fame = int.Parse(match.Groups["fame"].Value),
                            AccountBalance = int.Parse(match.Groups["accountBalance"].Value),
                            GoldBalance = int.Parse(match.Groups["goldBalance"].Value),
                            X = double.Parse(match.Groups["x"].Value),
                            Y = double.Parse(match.Groups["y"].Value),
                            Z = double.Parse(match.Groups["z"].Value)
                        });
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }

            return players;
        }

        static async Task Main(string[] args)
        {
            try
            {
                var mapExtractor = new ScumMapExtractor("island_4k.jpg");

                // Coordenada central - centro do mapa (setor B2)
                var centerCoord = ScumCoordinate.FromSectorCenter("B2");

                var list = @"
 1. kanfox
Steam: kanfox (76561198372541307)
Fame: 273                 
Account balance: 121063
Gold balance: 4
Location: X=-233789.594 Y=-48906.258 Z=35754.148

 2. GHOST
Steam: GHOST (76561199162601005)
Fame: 1046                
Account balance: 9381
Gold balance: 153
Location: X=-235860.313 Y=-62408.930 Z=38316.609

 3. Miguels
Steam: Miguels (76561198393076063)
Fame: 3526                
Account balance: 59598
Gold balance: 1158
Location: X=-167484.859 Y=441298.625 Z=72471.039

 4. JajaEuVolto
Steam: JajaEuVolto (76561198299977265)
Fame: 625                 
Account balance: 209506
Gold balance: 143
Location: X=269314.656 Y=509088.188 Z=21733.738

 5. HarysonFPS
Steam: HarysonFPS (76561198065306838)
Fame: 2290                
Account balance: 129915
Gold balance: 0
Location: X=-91006.297 Y=333766.844 Z=76922.609

 6. Ragnarok
Steam: Korosu 殺 (76561198002224431)
Fame: 2607                
Account balance: 208706
Gold balance: 15
Location: X=431209.000 Y=-834088.000 Z=2188.610
";

                var players = Parse(list).DistinctBy(x => x.SteamID);
                var json = JsonConvert.SerializeObject(players, Formatting.Indented);
                var d1 = new ScumCoordinate(120690.578, -808264.750, 13222.057);
                var d2 = new ScumCoordinate(125631.469, -813236.875, 13115.753);
                var distance = d1.DistanceTo(d2).ToString();
                Console.WriteLine(distance);

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
                    new ScumCoordinate(-250758.141f, -37223.129f)
                };
                var mid = ScumCoordinate.MidPoint((285811.59375f, -21049.46875f), (137592.1875f, -31476.5983f));
                var stream = await mapExtractor.ExtractMapWithPoints(
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
