using SixLabors.ImageSharp;

namespace ConsoleAppTest
{
    internal class Program
    {
        static void Main(string[] args)
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

                var points = new List<ScumCoordinate>
                {
                    new ScumCoordinate(137592.1875f, -31476.5983f, Color.Black.WithAlpha(200)),
                    new ScumCoordinate(285811.59375f, -21049.46875f, Color.Red.WithAlpha(200)),
                };
                var mid = ScumMapExtractor.GetMidpoint((285811.59375f, -21049.46875f), (137592.1875f, -31476.5983f));
                mapExtractor.ExtractMapWithPoints(
                    new ScumCoordinate(mid.x, mid.y),
                    points,
                    extractSize: 256,
                    outputPath: "scum_map_extracted.png"
                );

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
