using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

public class ScumMapExtractor
{
    private readonly string _mapImagePath;
    private readonly int _mapWidth;
    private readonly int _mapHeight;

    // Coordenadas reais do mapa SCUM (baseadas no fórum oficial)
    private const double MAP_X_MAX = 619646.8573;      // Esquerda do mapa
    private const double MAP_Y_MAX = 619659.7258;      // Topo do mapa
    private const double MAP_X_MIN = -905369.0266;     // Direita do mapa
    private const double MAP_Y_MIN = -904357.527;      // Base do mapa
    private const double TOTAL_MAP_LENGTH = 1524017.2528;
    private const double SECTOR_LENGTH = 304803.45056; // 3km por setor
    private const double HALF_SECTOR_LENGTH = 152401.72528;

    public ScumMapExtractor(string mapImagePath)
    {
        _mapImagePath = mapImagePath;

        // Carrega a imagem para obter dimensões
        using var image = Image.Load(mapImagePath);
        _mapWidth = image.Width;
        _mapHeight = image.Height;
    }

    //public static (float x, float y) GetMidpoint((float x1, float y1) point1, (float x2, float y2) point2)
    //{
    //    float midX = (point1.x1 + point2.x2) / 2;
    //    float midY = (point1.y1 + point2.y2) / 2;
    //    return (midX, midY);
    //}

    /// <summary>
    /// Extrai uma porção do mapa equivalente ao tamanho de um setor (3km x 3km)
    /// </summary>
    /// <param name="centerCoordinate">Coordenada central da extração</param>
    /// <param name="points">Lista de pontos para desenhar</param>
    /// <param name="outputStream">Stream para salvar a imagem</param>
    /// <param name="showGrid">Se true, desenha a grid dos setores</param>
    /// <param name="maintainAspectRatio">Se true, mantém proporção quadrada</param>
    public async Task ExtractSectorSizeArea(
        ScumCoordinate centerCoordinate,
        List<ScumCoordinate> points,
        Stream outputStream,
        bool showGrid = true,
        bool showLabels = true,
        bool maintainAspectRatio = true)
    {
        using var originalMap = Image.Load<Rgba32>(_mapImagePath);

        // Calcula o tamanho em pixels equivalente a um setor (3km)
        int sectorSizeInPixels = CalculateSectorSizeInPixels();

        // Desenha a grid dos setores primeiro (se solicitado)
        if (showGrid)
        {
            DrawSectorGrid(originalMap);
        }

        if (showLabels)
        {
            DrawSectorLabels(originalMap);
        }

        // Desenha todos os pontos no mapa original
        DrawPointsOnOriginalMap(originalMap, points);

        // Calcula área de extração centrada na coordenada, com tamanho de setor
        var centerPixel = GameCoordinateToPixel(centerCoordinate);
        var extractRect = CalculateExtractionRectangle(centerPixel, sectorSizeInPixels);

        // Extrai a porção do mapa
        using var extractedMap = ExtractMapPortion(originalMap, extractRect);

        // Redimensiona para manter proporção quadrada se solicitado
        Image<Rgba32> finalMap = extractedMap;
        if (maintainAspectRatio && extractRect.Width != extractRect.Height)
        {
            int size = Math.Max(extractRect.Width, extractRect.Height);
            finalMap = new Image<Rgba32>(size, size);
            finalMap.Mutate(ctx =>
            {
                ctx.Fill(Color.Black); // Fundo preto nas áreas vazias
                int offsetX = (size - extractedMap.Width) / 2;
                int offsetY = (size - extractedMap.Height) / 2;
                ctx.DrawImage(extractedMap, new Point(offsetX, offsetY), 1f);
            });
        }

        // Adiciona informações específicas do setor
        //AddSectorContextInfo(finalMap, centerCoordinate, points, sectorSizeInPixels);

        // Salva a imagem resultante
        await finalMap.SaveAsync(outputStream, new JpegEncoder());

        // Limpa recursos se criamos nova imagem
        if (finalMap != extractedMap)
        {
            finalMap.Dispose();
        }
    }

    /// <summary>
    /// Extrai um setor específico completo (ex: "B2", "A1", etc.)
    /// </summary>
    /// <param name="sectorReference">Referência do setor (ex: "B2")</param>
    /// <param name="points">Lista de pontos para desenhar</param>
    /// <param name="showGrid">Se true, desenha a grid dos setores</param>
    public async Task<Stream> ExtractCompleteSector(
        string sectorReference,
        List<ScumCoordinate> points,
        bool showGrid = true)
    {
        // Usa o centro do setor como coordenada central
        var sectorCenter = ScumCoordinate.FromSectorCenter(sectorReference);

        // Filtra pontos que estão dentro ou próximos do setor
        var relevantPoints = FilterPointsNearSector(points, sectorCenter);

        var memoryStream = new MemoryStream();
        await ExtractSectorSizeArea(sectorCenter, relevantPoints, memoryStream, showGrid, true);
        memoryStream.Position = 0;
        return memoryStream;
    }

    /// <summary>
    /// Calcula o tamanho em pixels equivalente a um setor (3km)
    /// </summary>
    private int CalculateSectorSizeInPixels()
    {
        // Pega dois pontos conhecidos para calcular a escala
        var point1 = new ScumCoordinate(0, 0);
        var point2 = new ScumCoordinate(SECTOR_LENGTH, 0); // 3km de distância

        var pixel1 = GameCoordinateToPixel(point1);
        var pixel2 = GameCoordinateToPixel(point2);

        // Calcula pixels por setor
        int pixelsPerSector = Math.Abs(pixel2.X - pixel1.X);

        return pixelsPerSector;
    }

    /// <summary>
    /// Filtra pontos que estão próximos de um setor específico
    /// </summary>
    private List<ScumCoordinate> FilterPointsNearSector(List<ScumCoordinate> allPoints, ScumCoordinate sectorCenter)
    {
        var relevantPoints = new List<ScumCoordinate>();
        double sectorRadius = SECTOR_LENGTH * 0.8; // 80% do tamanho do setor como raio

        foreach (var point in allPoints)
        {
            double distance = point.DistanceTo(sectorCenter);
            if (distance <= sectorRadius)
            {
                relevantPoints.Add(point);
            }
        }

        return relevantPoints;
    }

    /// <summary>
    /// Adiciona informações contextuais específicas para extração de setor
    /// </summary>
    private void AddSectorContextInfo(Image<Rgba32> extractedMap, ScumCoordinate centerCoordinate, List<ScumCoordinate> points, int sectorSize)
    {
        extractedMap.Mutate(ctx =>
        {
            var headerFont = SystemFonts.CreateFont("Arial", 16, FontStyle.Bold);
            var infoFont = SystemFonts.CreateFont("Arial", 12);

            // Fundo para informações
            var infoHeight = 90;
            ctx.Fill(Color.FromRgba(0, 0, 0, 220), new RectangleF(0, 0, extractedMap.Width, infoHeight));

            // Título destacando que é uma área de setor
            var sectorRef = centerCoordinate.GetSectorReference();
            ctx.DrawText($"SETOR {sectorRef} - Área 3km x 3km", headerFont, Color.Orange, new PointF(10, 8));

            // Coordenadas do centro
            var centerText = $"Centro: ({centerCoordinate.X:F0}, {centerCoordinate.Y:F0})";
            ctx.DrawText(centerText, infoFont, Color.White, new PointF(10, 30));

            // Informações da extração
            var extractInfo = $"Tamanho: {sectorSize}x{sectorSize}px | Pontos: {points.Count}";
            ctx.DrawText(extractInfo, infoFont, Color.LightGray, new PointF(10, 48));

            // Escala exata
            var scaleText = $"Escala: 1 setor = 3km | {sectorSize}px = 304.803m";
            ctx.DrawText(scaleText, infoFont, Color.Yellow, new PointF(10, 66));
        });
    }
    /// <param name="centerCoordinate">Coordenada central (em coordenadas reais do SCUM)</param>
    /// <param name="points">Lista de pontos para desenhar no mapa</param>
    /// <param name="extractSize">Tamanho da área a extrair em pixels</param>
    /// <param name="autoFitPoints">Se true, ajusta automaticamente a área para incluir todos os pontos</param>
    /// <param name="showGrid">Se true, desenha a grid dos setores</param>
    public async Task<Stream> ExtractMapWithPoints(
        ScumCoordinate centerCoordinate,
        List<ScumCoordinate> points,
        int extractSize = 1024,
        bool autoFitPoints = true,
        bool showLabels = true,
        bool showGrid = true)
    {
        using var originalMap = Image.Load<Rgba32>(_mapImagePath);

        // Desenha a grid dos setores primeiro (se solicitado)
        if (showGrid)
        {
            DrawSectorGrid(originalMap, lineWidth: 0.5f);
        }

        if (showLabels)
        {
            DrawSectorLabels(originalMap);
        }

        // Desenha todos os pontos no mapa original
        DrawPointsOnOriginalMap(originalMap, points);

        Rectangle extractRect;

        if (autoFitPoints && points.Count > 0)
        {
            // Calcula área que inclui todos os pontos + margem
            extractRect = CalculateOptimalExtractionArea(centerCoordinate, points, extractSize);
        }
        else
        {
            // Usa área centrada na coordenada especificada
            var centerPixel = GameCoordinateToPixel(centerCoordinate);
            extractRect = CalculateExtractionRectangle(centerPixel, extractSize);
        }

        // Extrai a porção do mapa (já com pontos desenhados)
        using var extractedMap = ExtractMapPortion(originalMap, extractRect);

        // Adiciona informações de contexto
        //AddContextInfo(extractedMap, centerCoordinate, points, extractRect);

        var memoryStream = new MemoryStream();
        await extractedMap.SaveAsync(memoryStream, new JpegEncoder());
        memoryStream.Position = 0;

        return memoryStream;

        // Salva a imagem resultante
    }

    /// <summary>
    /// Converte coordenadas reais do SCUM para pixels da imagem
    /// </summary>
    private Point GameCoordinateToPixel(ScumCoordinate gameCoord)
    {
        // Normaliza as coordenadas para 0-1
        double normalizedX = (gameCoord.X - MAP_X_MIN) / (MAP_X_MAX - MAP_X_MIN);
        double normalizedY = (gameCoord.Y - MAP_Y_MIN) / (MAP_Y_MAX - MAP_Y_MIN);

        // Inverte Y porque imagens têm origem no topo
        normalizedY = 1.0 - normalizedY;
        normalizedX = 1.0 - normalizedX;

        int pixelX = (int)(normalizedX * _mapWidth);
        int pixelY = (int)(normalizedY * _mapHeight);

        // Garante que os pixels estão dentro dos limites
        pixelX = Math.Clamp(pixelX, 0, _mapWidth - 1);
        pixelY = Math.Clamp(pixelY, 0, _mapHeight - 1);

        return new Point(pixelX, pixelY);
    }

    /// <summary>
    /// Calcula o retângulo de extração centrado no ponto especificado
    /// </summary>
    private Rectangle CalculateExtractionRectangle(Point centerPixel, int size)
    {
        int halfSize = size / 2;

        int x = Math.Max(0, centerPixel.X - halfSize);
        int y = Math.Max(0, centerPixel.Y - halfSize);

        int width = Math.Min(size, _mapWidth - x);
        int height = Math.Min(size, _mapHeight - y);

        return new Rectangle(x, y, width, height);
    }

    /// <summary>
    /// Extrai uma porção específica do mapa
    /// </summary>
    private Image<Rgba32> ExtractMapPortion(Image<Rgba32> originalMap, Rectangle extractRect)
    {
        var extractedMap = new Image<Rgba32>(extractRect.Width, extractRect.Height);

        extractedMap.Mutate(ctx => ctx
            .DrawImage(originalMap, new Point(-extractRect.X, -extractRect.Y), 1f));

        return extractedMap;
    }

    /// <summary>
    /// Desenha a grid dos setores do SCUM no mapa
    /// </summary>
    /// <param name="map">Imagem do mapa onde desenhar a grid</param>
    /// <param name="gridColor">Cor das linhas da grid</param>
    /// <param name="lineWidth">Espessura das linhas</param>
    /// <param name="showLabels">Se true, mostra os labels dos setores</param>
    public void DrawSectorGrid(Image<Rgba32> map, Color? gridColor = null, float lineWidth = 1f)
    {
        gridColor ??= Color.Black;
        map.Mutate(ctx =>
        {
            // Desenha linhas verticais (separando colunas 0,1,2,3,4)
            foreach (var kvp in SectorDefinitions.SECTOR_X_CENTERS)
            {
                int number = kvp.Key;
                double worldX = kvp.Value;

                // Calcula as bordas do setor (metade do tamanho do setor para cada lado)
                double leftBorder = worldX - HALF_SECTOR_LENGTH;
                double rightBorder = worldX + HALF_SECTOR_LENGTH;

                // Converte para pixels
                var leftPixel = GameCoordinateToPixel(new ScumCoordinate(leftBorder, 0));
                var rightPixel = GameCoordinateToPixel(new ScumCoordinate(rightBorder, 0));

                // Desenha bordas esquerda e direita do setor
                if (leftPixel.X >= 0 && leftPixel.X < map.Width)
                {
                    ctx.DrawLine(gridColor.Value, lineWidth,
                        new PointF(leftPixel.X, 0),
                        new PointF(leftPixel.X, map.Height));
                }

                if (rightPixel.X >= 0 && rightPixel.X < map.Width)
                {
                    ctx.DrawLine(gridColor.Value, lineWidth,
                        new PointF(rightPixel.X, 0),
                        new PointF(rightPixel.X, map.Height));
                }
            }

            // Desenha linhas horizontais (separando linhas Z,A,B,C,D)
            foreach (var kvp in SectorDefinitions.SECTOR_Y_CENTERS)
            {
                char letter = kvp.Key;
                double worldY = kvp.Value;

                // Calcula as bordas do setor
                double topBorder = worldY + HALF_SECTOR_LENGTH;
                double bottomBorder = worldY - HALF_SECTOR_LENGTH;

                // Converte para pixels
                var topPixel = GameCoordinateToPixel(new ScumCoordinate(0, topBorder));
                var bottomPixel = GameCoordinateToPixel(new ScumCoordinate(0, bottomBorder));

                // Desenha bordas superior e inferior do setor
                if (topPixel.Y >= 0 && topPixel.Y < map.Height)
                {
                    ctx.DrawLine(gridColor.Value, lineWidth,
                        new PointF(0, topPixel.Y),
                        new PointF(map.Width, topPixel.Y));
                }

                if (bottomPixel.Y >= 0 && bottomPixel.Y < map.Height)
                {
                    ctx.DrawLine(gridColor.Value, lineWidth,
                        new PointF(0, bottomPixel.Y),
                        new PointF(map.Width, bottomPixel.Y));
                }
            }

        });
    }

    private void DrawSectorLabels(Image<Rgba32> map)
    {
        var color = Color.FromRgba(255, 255, 255, 180); // Amarelo semi-transparente
        var labelColor = Color.White;
        var font = SystemFonts.CreateFont("Arial", 20, FontStyle.Regular);

        map.Mutate(ctx =>
        {
            foreach (var letterKvp in SectorDefinitions.SECTOR_Y_CENTERS)
            {
                foreach (var numberKvp in SectorDefinitions.SECTOR_X_CENTERS)
                {
                    char letter = letterKvp.Key;
                    int number = numberKvp.Key;
                    string sectorLabel = $"{letter}{number}";

                    // Usa o centro do setor para posicionar a label
                    var sectorCenter = new ScumCoordinate(numberKvp.Value, letterKvp.Value);
                    var centerPixel = GameCoordinateToPixel(sectorCenter);

                    // Verifica se está dentro da imagem com margem
                    if (centerPixel.X >= 50 && centerPixel.X < map.Width - 50 &&
                        centerPixel.Y >= 50 && centerPixel.Y < map.Height - 50)
                    {
                        // Calcula aproximadamente o canto superior esquerdo do setor
                        // Como cada setor tem tamanho fixo, calculamos offset do centro
                        var sectorSizePixels = CalculateSectorSizeInPixels();
                        var halfSectorPixels = sectorSizePixels / 2;

                        // Posição do canto superior esquerdo (relativo ao centro)
                        var labelX = centerPixel.X - halfSectorPixels + 8;  // 8px de margem
                        var labelY = centerPixel.Y - halfSectorPixels + 8;  // 8px de margem

                        var textPosition = new PointF(labelX, labelY);

                        // Mede o texto para o fundo
                        var textSize = TextMeasurer.MeasureSize(sectorLabel, new TextOptions(font));

                        // Fundo semi-transparente para o texto
                        var bgRect = new RectangleF(
                            textPosition.X - 4,
                            textPosition.Y - 2,
                            textSize.Width + 8,
                            textSize.Height + 4
                        );
                        ctx.Fill(Color.FromRgba(0, 0, 0, 180), bgRect);

                        // Borda sutil no fundo para destacar mais
                        ctx.Draw(Color.FromRgba(255, 255, 0, 100), 1f, bgRect);

                        // Desenha o texto
                        ctx.DrawText(sectorLabel, font, labelColor, textPosition);
                    }
                }
            }
        });
    }

    /// <summary>
    /// Cria um mapa completo apenas com a grid dos setores (útil para referência)
    /// </summary>
    /// <param name="outputPath">Caminho para salvar o mapa com grid</param>
    /// <param name="gridColor">Cor das linhas da grid</param>
    /// <param name="backgroundColor">Cor de fundo (null para manter original)</param>
    public void CreateGridOnlyMap(string outputPath = "scum_grid_map.png", Color? gridColor = null, Color? backgroundColor = null)
    {
        using var originalMap = Image.Load<Rgba32>(_mapImagePath);

        // Aplica cor de fundo se especificada
        if (backgroundColor.HasValue)
        {
            originalMap.Mutate(ctx => ctx.BackgroundColor(backgroundColor.Value));
        }

        // Desenha a grid
        DrawSectorGrid(originalMap, gridColor, lineWidth: 1f);
        DrawSectorLabels(originalMap);

        // Adiciona título
        originalMap.Mutate(ctx =>
        {
            var titleFont = SystemFonts.CreateFont("Arial", 32, FontStyle.Bold);
            var title = "SCUM - Grid dos Setores";
            var titleSize = TextMeasurer.MeasureSize(title, new TextOptions(titleFont));

            // Fundo para o título
            var titleBg = new RectangleF(10, 10, titleSize.Width + 20, titleSize.Height + 10);
            ctx.Fill(Color.FromRgba(0, 0, 0, 200), titleBg);
            ctx.DrawText(title, titleFont, Color.White, new PointF(20, 15));

            // Legenda
            var legendFont = SystemFonts.CreateFont("Arial", 16);
            var legendY = titleSize.Height + 35;

            ctx.DrawText("• Cada setor = 3km x 3km", legendFont, Color.Yellow, new PointF(20, legendY));
            ctx.DrawText("• Numeração: 0-4 (direita → esquerda) | Letras: Z-D (sul → norte)", legendFont, Color.LightGray, new PointF(20, legendY + 25));
            ctx.DrawText("• Coordenadas baseadas no fórum oficial SCUM", legendFont, Color.LightGray, new PointF(20, legendY + 50));
        });

        originalMap.Save(outputPath);
        Console.WriteLine($"Mapa com grid salvo em: {outputPath}");
    }
    private Rectangle CalculateOptimalExtractionArea(ScumCoordinate centerCoordinate, List<ScumCoordinate> points, int minSize)
    {
        var allPoints = new List<ScumCoordinate>(points) { centerCoordinate };
        var pixels = allPoints.Select(GameCoordinateToPixel).ToList();

        // Encontra limites de todos os pontos
        int minX = pixels.Min(p => p.X);
        int maxX = pixels.Max(p => p.X);
        int minY = pixels.Min(p => p.Y);
        int maxY = pixels.Max(p => p.Y);

        // Adiciona margem de 10%
        int margin = Math.Max(50, Math.Max(maxX - minX, maxY - minY) / 10);

        minX = Math.Max(0, minX - margin);
        maxX = Math.Min(_mapWidth - 1, maxX + margin);
        minY = Math.Max(0, minY - margin);
        maxY = Math.Min(_mapHeight - 1, maxY + margin);

        int width = maxX - minX;
        int height = maxY - minY;

        // Garante tamanho mínimo
        if (width < minSize || height < minSize)
        {
            var centerPixel = GameCoordinateToPixel(centerCoordinate);
            return CalculateExtractionRectangle(centerPixel, minSize);
        }

        return new Rectangle(minX, minY, width, height);
    }

    /// <summary>
    /// Calcula distância entre dois pontos
    /// </summary>
    private float CalculateDistance(PointF point1, PointF point2)
    {
        float deltaX = point2.X - point1.X;
        float deltaY = point2.Y - point1.Y;
        return (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    /// <summary>
    /// Estrutura para armazenar dados de pontos ajustados
    /// </summary>
    private class AdjustedPoint
    {
        public ScumCoordinate OriginalCoordinate { get; set; }
        public Color Color { get; set; }
        public PointF PixelPosition { get; set; }
        public string? Label { get; internal set; }
    }


    /// <summary>
    /// Resolve sobreposições de pontos reposicionando-os
    /// </summary>
    private List<AdjustedPoint> ResolveOverlappingPoints(List<ScumCoordinate> points)
    {
        var adjustedPoints = new List<AdjustedPoint>();
        const int minDistance = 25; // Distância mínima em pixels entre pontos
        const int maxIterations = 100; // Evita loop infinito

        // Converte todos os pontos para pixels
        var allCoordinates = new List<ScumCoordinate>(points);

        foreach (var coord in allCoordinates)
        {
            var pixel = GameCoordinateToPixel(coord);
            adjustedPoints.Add(new AdjustedPoint
            {
                OriginalCoordinate = coord,
                PixelPosition = new PointF(pixel.X, pixel.Y)
            });
        }

        // Algoritmo de separação de pontos
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            bool hadCollision = false;

            for (int i = 0; i < adjustedPoints.Count; i++)
            {
                for (int j = i + 1; j < adjustedPoints.Count; j++)
                {
                    var point1 = adjustedPoints[i];
                    var point2 = adjustedPoints[j];

                    float distance = CalculateDistance(point1.PixelPosition, point2.PixelPosition);

                    if (distance < minDistance)
                    {
                        hadCollision = true;

                        // Calcula vetor de separação
                        float deltaX = point2.PixelPosition.X - point1.PixelPosition.X;
                        float deltaY = point2.PixelPosition.Y - point1.PixelPosition.Y;

                        // Evita divisão por zero
                        if (Math.Abs(deltaX) < 0.1f && Math.Abs(deltaY) < 0.1f)
                        {
                            deltaX = 1f;
                            deltaY = 0f;
                        }

                        float length = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                        deltaX /= length;
                        deltaY /= length;

                        // Calcula o deslocamento necessário
                        float overlap = minDistance - distance;
                        float moveDistance = overlap / 2f;

                        // Move os pontos para lados opostos
                        // Ponto central tem prioridade (move menos)
                        float factor1 = 0.8f;
                        float factor2 = 0.8f;

                        adjustedPoints[i] = new AdjustedPoint
                        {
                            OriginalCoordinate = point1.OriginalCoordinate,
                            PixelPosition = new PointF(
                                point1.PixelPosition.X - deltaX * moveDistance * factor1,
                                point1.PixelPosition.Y - deltaY * moveDistance * factor1
                            ),
                            Color = point1.Color,
                            Label = point1.Label
                        };

                        adjustedPoints[j] = new AdjustedPoint
                        {
                            OriginalCoordinate = point2.OriginalCoordinate,
                            PixelPosition = new PointF(
                                point2.PixelPosition.X + deltaX * moveDistance * factor2,
                                point2.PixelPosition.Y + deltaY * moveDistance * factor2
                            ),
                            Color = point2.Color,
                            Label = point2.Label
                        };
                    }
                }
            }

            // Se não houve colisões, termina
            if (!hadCollision)
                break;
        }

        return adjustedPoints;
    }

    /// <summary>
    /// Desenha todos os pontos no mapa original
    /// </summary>
    private void DrawPointsOnOriginalMap(Image<Rgba32> originalMap, List<ScumCoordinate> points)
    {
        // Resolve sobreposições antes de desenhar
        //var adjustedPoints = ResolveOverlappingPoints(points);

        originalMap.Mutate(ctx =>
        {
            // Desenha pontos
            foreach (var pointData in points)
            {
                var pointPixel = GameCoordinateToPixel(pointData);

                ctx.Fill(Color.White, new EllipsePolygon(pointPixel.X, pointPixel.Y, 10f));
                ctx.Fill(pointData.Color, new EllipsePolygon(pointPixel.X, pointPixel.Y, 8f));

                //// Linha conectando posição original com ajustada (se moveu)
                //var originalPointPixel = GameCoordinateToPixel(pointData.OriginalCoordinate);
                //if (Math.Abs(pointPixel.X - originalPointPixel.X) > 2 || Math.Abs(pointPixel.Y - originalPointPixel.Y) > 2)
                //{
                //    ctx.DrawLine(Color.FromRgba(0, 0, 255, 150), 1f,
                //        new PointF(originalPointPixel.X, originalPointPixel.Y),
                //        new PointF(pointPixel.X, pointPixel.Y));
                //}

                //if (!string.IsNullOrEmpty(pointData.Label))
                //{
                //    // Label com setor
                //    var font = SystemFonts.CreateFont("Arial", 10, FontStyle.Regular);
                //    var sectorText = pointData.OriginalCoordinate.GetSectorReference();
                //    if (!string.IsNullOrEmpty(sectorText))
                //    {
                //        // Fundo semi-transparente para o texto
                //        var textSize = TextMeasurer.MeasureSize(sectorText, new TextOptions(font));
                //        var textRect = new RectangleF(pointPixel.X + 15, pointPixel.Y - 10, textSize.Width + 4, textSize.Height + 2);
                //        ctx.Fill(Color.FromRgba(0, 0, 0, 180), textRect);
                //        ctx.DrawText(sectorText, font, Color.White, new PointF(pointPixel.X + 17, pointPixel.Y - 8));
                //    }
                //}

            }
        });
    }

    /// <summary>
    /// Adiciona informações de contexto na imagem extraída
    /// </summary>
    private void AddContextInfo(Image<Rgba32> extractedMap, ScumCoordinate centerCoordinate, List<ScumCoordinate> points, Rectangle extractRect)
    {
        extractedMap.Mutate(ctx =>
        {
            var headerFont = SystemFonts.CreateFont("Arial", 14, FontStyle.Bold);
            var infoFont = SystemFonts.CreateFont("Arial", 11);

            // Fundo para informações
            var infoHeight = 80;
            ctx.Fill(Color.FromRgba(0, 0, 0, 200), new RectangleF(0, 0, extractedMap.Width, infoHeight));

            // Título
            ctx.DrawText("SCUM Map Extract", headerFont, Color.Yellow, new PointF(10, 8));

            // Informações do centro
            var centerText = $"Centro: {centerCoordinate.GetSectorReference()} ({centerCoordinate.X:F0}, {centerCoordinate.Y:F0})";
            ctx.DrawText(centerText, infoFont, Color.White, new PointF(10, 28));

            // Contagem de pontos
            var pointsText = $"Pontos: {points.Count} | Área: {extractRect.Width}x{extractRect.Height}px";
            ctx.DrawText(pointsText, infoFont, Color.LightGray, new PointF(10, 45));

            // Escala aproximada
            double metersPerPixel = SECTOR_LENGTH / (Math.Min(extractRect.Width, extractRect.Height) / 3.0);
            var scaleText = $"Escala: ~{metersPerPixel:F0}m/pixel";
            ctx.DrawText(scaleText, infoFont, Color.LightGray, new PointF(10, 62));
        });
    }
}

public static class SectorDefinitions
{
    // Centros dos setores (baseado nos dados do fórum SCUM)
    public static readonly Dictionary<char, double> SECTOR_Y_CENTERS = new()
    {
        { 'D', 467258.00052 },
        { 'C', 162454.54996 },
        { 'B', -142348.9006 },
        { 'A', -447664.53521 },
        { 'Z', -752467.94021 }
    };

    public static readonly Dictionary<int, double> SECTOR_X_CENTERS = new()
    {
        { 4, 467245.13202 },
        { 3, 162441.68146 },
        { 2, -142861.08465 },
        { 1, -447664.53521 },
        { 0, -752467.98577 }
    };
}

/// <summary>
/// Representa uma coordenada no mundo do SCUM usando o sistema real de coordenadas
/// </summary>
public struct ScumCoordinate
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public string? Label { get; set; }
    public Color Color { get; set; }


    public ScumCoordinate(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
        Color = Color.Red;
    }
    public ScumCoordinate(double x, double y)
    {
        X = x;
        Y = y;
        Color = Color.Red;
    }

    public ScumCoordinate(double x, double y, Color color)
    {
        X = x;
        Y = y;
        Color = color;
    }

    public ScumCoordinate WithLabel(string label)
    {
        Label = label;
        return this;
    }

    /// <summary>
    /// Calcula a distância em metros até outra coordenada
    /// </summary>
    public double DistanceTo(ScumCoordinate target) => Math.Sqrt(
        Math.Pow(target.X - X, 2) +
        Math.Pow(target.Y - Y, 2) +
        Math.Pow(target.Z - Z, 2));

    public override readonly string ToString() => $"X={X} Y={Y} Z={Z}".Replace(",", ".");

    public static ScumCoordinate MidPoint((float x1, float y1) point1, (float x2, float y2) point2)
    {
        float midX = (point1.x1 + point2.x2) / 2;
        float midY = (point1.y1 + point2.y2) / 2;
        return new ScumCoordinate(midX, midY);
    }

    /// <summary>
    /// Cria coordenada a partir do centro de um setor (ex: "B2")
    /// </summary>
    public static ScumCoordinate FromSectorCenter(string sectorRef)
    {
        if (string.IsNullOrEmpty(sectorRef) || sectorRef.Length != 2)
            throw new ArgumentException("Referência de setor inválida. Use formato como 'B2'");

        char letter = char.ToUpper(sectorRef[0]);
        if (!int.TryParse(sectorRef[1].ToString(), out int number))
            throw new ArgumentException("Número do setor inválido");

        if (!SectorDefinitions.SECTOR_Y_CENTERS.ContainsKey(letter))
            throw new ArgumentException($"Letra do setor inválida: {letter}. Use Z, A, B, C ou D");

        if (!SectorDefinitions.SECTOR_X_CENTERS.ContainsKey(number))
            throw new ArgumentException($"Número do setor inválido: {number}. Use 0, 1, 2, 3 ou 4 (0=direita, 4=esquerda)");

        double x = SectorDefinitions.SECTOR_X_CENTERS[number];
        double y = SectorDefinitions.SECTOR_Y_CENTERS[letter];

        return new ScumCoordinate(x, y);
    }

    /// <summary>
    /// Cria coordenada com offset a partir do centro de um setor
    /// </summary>
    public static ScumCoordinate FromSectorWithOffset(string sectorRef, double offsetX, double offsetY)
    {
        var sectorCenter = FromSectorCenter(sectorRef);
        return new ScumCoordinate(sectorCenter.X + offsetX, sectorCenter.Y + offsetY);
    }

    /// <summary>
    /// Tenta determinar em qual setor esta coordenada está localizada
    /// </summary>
    public string GetSectorReference()
    {
        // Encontra o setor mais próximo
        char? closestLetter = null;
        int? closestNumber = null;
        double minDistance = double.MaxValue;

        foreach (var letterPair in SectorDefinitions.SECTOR_Y_CENTERS)
        {
            foreach (var numberPair in SectorDefinitions.SECTOR_X_CENTERS)
            {
                double distance = Math.Sqrt(
                    Math.Pow(X - numberPair.Value, 2) +
                    Math.Pow(Y - letterPair.Value, 2));

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestLetter = letterPair.Key;
                    closestNumber = numberPair.Key;
                }
            }
        }

        return closestLetter.HasValue && closestNumber.HasValue
            ? $"{closestLetter}{closestNumber}"
            : "?";
    }


    //public override string ToString()
    //{
    //    return $"({X:F1}, {Y:F1}) [{GetSectorReference()}]";
    //}
}