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


        static async Task Main(string[] args)
        {
            try
            {
                var mapExtractor = new ScumMapExtractor("island_4k.jpg");

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
