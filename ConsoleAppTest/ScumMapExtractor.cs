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
        //var relevantPoints = FilterPointsNearSector(points, sectorCenter);

        var memoryStream = new MemoryStream();
        await ExtractSectorSizeAreaWithWatermark(sectorCenter, points, memoryStream, showGrid: showGrid, showLabels: true);
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

    /// <param name="centerCoordinate">Coordenada central (em coordenadas reais do SCUM)</param>
    /// <param name="points">Lista de pontos para desenhar no mapa</param>
    /// <param name="extractSize">Tamanho da área a extrair em pixels</param>
    /// <param name="autoFitPoints">Se true, ajusta automaticamente a área para incluir todos os pontos</param>
    /// <param name="showGrid">Se true, desenha a grid dos setores</param>
    public async Task<Stream> ExtractMapWithPoints(
        ScumCoordinate centerCoordinate,
        List<ScumCoordinate> points,
        int extractSize = 512,
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
    /// Watermark position options
    /// </summary>
    public enum WatermarkPosition
    {
        TopLeft,
        TopRight,
        TopCenter,
        BottomLeft,
        BottomRight,
        BottomCenter,
        Center,
        CenterLeft,
        CenterRight
    }

    /// <summary>
    /// Applies a PNG watermark to the extracted map
    /// </summary>
    /// <param name="extractedMap">The map image to apply watermark to</param>
    /// <param name="watermarkPath">Path to the PNG watermark file</param>
    /// <param name="position">Position where to place the watermark</param>
    /// <param name="opacity">Opacity of the watermark (0.0 to 1.0)</param>
    /// <param name="scale">Scale factor for the watermark (1.0 = original size)</param>
    /// <param name="margin">Margin from edges in pixels</param>
    private void ApplyWatermark(Image<Rgba32> extractedMap, string watermarkPath,
        WatermarkPosition position = WatermarkPosition.BottomRight,
        float opacity = 0.7f,
        float scale = 1.0f,
        int margin = 10)
    {
        if (!File.Exists(watermarkPath))
        {
            throw new FileNotFoundException($"Watermark file not found: {watermarkPath}");
        }

        using var watermark = Image.Load<Rgba32>(watermarkPath);
        ApplyWatermark(extractedMap, watermark, position, opacity, scale, margin);
    }

    /// <summary>
    /// Applies a watermark image to the extracted map
    /// </summary>
    /// <param name="extractedMap">The map image to apply watermark to</param>
    /// <param name="watermark">The watermark image</param>
    /// <param name="position">Position where to place the watermark</param>
    /// <param name="opacity">Opacity of the watermark (0.0 to 1.0)</param>
    /// <param name="scale">Scale factor for the watermark (1.0 = original size)</param>
    /// <param name="margin">Margin from edges in pixels</param>
    private void ApplyWatermark(Image<Rgba32> extractedMap, Image<Rgba32> watermark,
        WatermarkPosition position = WatermarkPosition.BottomRight,
        float opacity = 0.7f,
        float scale = 1.0f,
        int margin = 10)
    {
        // Scale watermark if needed
        Image<Rgba32> scaledWatermark = watermark;
        if (Math.Abs(scale - 1.0f) > 0.001f)
        {
            int newWidth = (int)(watermark.Width * scale);
            int newHeight = (int)(watermark.Height * scale);
            scaledWatermark = watermark.Clone();
            scaledWatermark.Mutate(ctx => ctx.Resize(newWidth, newHeight));
        }

        try
        {
            // Calculate position
            var watermarkPosition = CalculateWatermarkPosition(
                extractedMap.Width, extractedMap.Height,
                scaledWatermark.Width, scaledWatermark.Height,
                position, margin);

            // Apply watermark
            extractedMap.Mutate(ctx =>
            {
                ctx.DrawImage(scaledWatermark, watermarkPosition, opacity);
            });
        }
        finally
        {
            // Dispose scaled watermark if it was created
            if (scaledWatermark != watermark)
            {
                scaledWatermark.Dispose();
            }
        }
    }

    /// <summary>
    /// Calculate the position for watermark placement
    /// </summary>
    private Point CalculateWatermarkPosition(int mapWidth, int mapHeight,
        int watermarkWidth, int watermarkHeight,
        WatermarkPosition position, int margin)
    {
        return position switch
        {
            WatermarkPosition.TopLeft => new Point(margin, margin),

            WatermarkPosition.TopRight => new Point(
                mapWidth - watermarkWidth - margin, margin),

            WatermarkPosition.TopCenter => new Point(
                (mapWidth - watermarkWidth) / 2, margin),

            WatermarkPosition.BottomLeft => new Point(
                margin, mapHeight - watermarkHeight - margin),

            WatermarkPosition.BottomRight => new Point(
                mapWidth - watermarkWidth - margin,
                mapHeight - watermarkHeight - margin),

            WatermarkPosition.BottomCenter => new Point(
                (mapWidth - watermarkWidth) / 2,
                mapHeight - watermarkHeight - margin),

            WatermarkPosition.Center => new Point(
                (mapWidth - watermarkWidth) / 2,
                (mapHeight - watermarkHeight) / 2),

            WatermarkPosition.CenterLeft => new Point(
                margin, (mapHeight - watermarkHeight) / 2),

            WatermarkPosition.CenterRight => new Point(
                mapWidth - watermarkWidth - margin,
                (mapHeight - watermarkHeight) / 2),

            _ => new Point(mapWidth - watermarkWidth - margin,
                          mapHeight - watermarkHeight - margin)
        };
    }

    /// <summary>
    /// Enhanced method that applies watermark with automatic sizing based on map size
    /// </summary>
    private void ApplyAdaptiveWatermark(Image<Rgba32> extractedMap,
        WatermarkPosition position = WatermarkPosition.BottomRight,
        float opacity = 0.7f,
        float maxSizePercent = 0.2f, // Max 20% of map size
        int margin = 10)
    {
        string watermarkPath = "thescumbot.png";
        if (!File.Exists(watermarkPath))
        {
            throw new FileNotFoundException($"Watermark file not found: {watermarkPath}");
        }

        using var watermark = Image.Load<Rgba32>(watermarkPath);

        // Calculate adaptive scale
        float maxWidth = extractedMap.Width * maxSizePercent;
        float maxHeight = extractedMap.Height * maxSizePercent;

        float scaleX = maxWidth / watermark.Width;
        float scaleY = maxHeight / watermark.Height;
        float adaptiveScale = Math.Min(scaleX, scaleY);

        // Don't upscale, only downscale
        adaptiveScale = Math.Min(adaptiveScale, 1.0f);

        ApplyWatermark(extractedMap, watermark, position, opacity, adaptiveScale, margin);
    }

    /// <summary>
    /// Applies multiple watermarks (useful for corners, credits, etc.)
    /// </summary>
    private void ApplyMultipleWatermarks(Image<Rgba32> extractedMap,
        Dictionary<string, (WatermarkPosition position, float opacity, float scale)> watermarks,
        int margin = 10)
    {
        foreach (var watermarkConfig in watermarks)
        {
            string watermarkPath = watermarkConfig.Key;
            var (position, opacity, scale) = watermarkConfig.Value;

            if (File.Exists(watermarkPath))
            {
                ApplyWatermark(extractedMap, watermarkPath, position, opacity, scale, margin);
            }
        }
    }

    /// <summary>
    /// Creates a text watermark as an image
    /// </summary>
    private Image<Rgba32> CreateTextWatermark(string text, Color textColor,
        int fontSize = 20, string fontFamily = "Arial", FontStyle fontStyle = FontStyle.Regular)
    {
        var font = SystemFonts.CreateFont(fontFamily, fontSize, fontStyle);
        var textOptions = new TextOptions(font);
        var textSize = TextMeasurer.MeasureSize(text, textOptions);

        // Add padding
        int padding = 10;
        int imageWidth = (int)textSize.Width + (padding * 2);
        int imageHeight = (int)textSize.Height + (padding * 2);

        var textImage = new Image<Rgba32>(imageWidth, imageHeight);
        textImage.Mutate(ctx =>
        {
            // Transparent background
            ctx.Fill(Color.Transparent);

            // Draw text
            ctx.DrawText(text, font, textColor, new PointF(padding, padding));
        });

        return textImage;
    }

    /// <summary>
    /// Applies a text watermark
    /// </summary>
    private void ApplyTextWatermark(Image<Rgba32> extractedMap, string text,
        WatermarkPosition position = WatermarkPosition.BottomRight,
        Color? textColor = null, float opacity = 0.7f,
        int fontSize = 16, int margin = 10)
    {
        textColor ??= Color.White;

        using var textWatermark = CreateTextWatermark(text, textColor.Value, fontSize);
        ApplyWatermark(extractedMap, textWatermark, position, opacity, 1.0f, margin);
    }

    /// <summary>
    /// Updated ExtractSectorSizeArea method with watermark support
    /// </summary>
    public async Task ExtractSectorSizeAreaWithWatermark(
        ScumCoordinate centerCoordinate,
        List<ScumCoordinate> points,
        Stream outputStream,
        WatermarkPosition watermarkPosition = WatermarkPosition.BottomRight,
        float watermarkOpacity = 0.7f,
        bool showGrid = true,
        bool showLabels = true,
        bool maintainAspectRatio = true)
    {
        using var originalMap = Image.Load<Rgba32>(_mapImagePath);

        // Calculate sector size and draw elements (same as before)
        int sectorSizeInPixels = CalculateSectorSizeInPixels();

        if (showGrid)
        {
            DrawSectorGrid(originalMap);
        }

        if (showLabels)
        {
            DrawSectorLabels(originalMap);
        }

        DrawPointsOnOriginalMapWithOverlapResolution(originalMap, points);

        // Extract the map portion
        var centerPixel = GameCoordinateToPixel(centerCoordinate);
        var extractRect = CalculateExtractionRectangle(centerPixel, sectorSizeInPixels);

        using var extractedMap = ExtractMapPortion(originalMap, extractRect);

        Image<Rgba32> finalMap = extractedMap;
        if (maintainAspectRatio && extractRect.Width != extractRect.Height)
        {
            int size = Math.Max(extractRect.Width, extractRect.Height);
            finalMap = new Image<Rgba32>(size, size);
            finalMap.Mutate(ctx =>
            {
                ctx.Fill(Color.Black);
                int offsetX = (size - extractedMap.Width) / 2;
                int offsetY = (size - extractedMap.Height) / 2;
                ctx.DrawImage(extractedMap, new Point(offsetX, offsetY), 1f);
            });
        }

        ApplyAdaptiveWatermark(finalMap, watermarkPosition, watermarkOpacity);

        // Save the final image
        await finalMap.SaveAsync(outputStream, new JpegEncoder());

        if (finalMap != extractedMap)
        {
            finalMap.Dispose();
        }
    }

    /// <summary>
    /// Updated ExtractMapWithPoints method with watermark support
    /// </summary>
    public async Task<Stream> ExtractMapWithPointsWithWatermark(
        ScumCoordinate centerCoordinate,
        List<ScumCoordinate> points,
        int extractSize = 512,
        bool autoFitPoints = true,
        bool showLabels = true,
        bool showGrid = true,
        WatermarkPosition watermarkPosition = WatermarkPosition.BottomRight,
        float watermarkOpacity = 0.7f)
    {
        using var originalMap = Image.Load<Rgba32>(_mapImagePath);

        if (showGrid)
        {
            DrawSectorGrid(originalMap, lineWidth: 0.5f);
        }

        if (showLabels)
        {
            DrawSectorLabels(originalMap);
        }

        DrawPointsOnOriginalMapWithOverlapResolution(originalMap, points);

        Rectangle extractRect;
        if (autoFitPoints && points.Count > 0)
        {
            extractRect = CalculateOptimalExtractionArea(centerCoordinate, points, extractSize);
        }
        else
        {
            var centerPixel = GameCoordinateToPixel(centerCoordinate);
            extractRect = CalculateExtractionRectangle(centerPixel, extractSize);
        }

        using var extractedMap = ExtractMapPortion(originalMap, extractRect);

        ApplyAdaptiveWatermark(extractedMap, watermarkPosition, watermarkOpacity);

        var memoryStream = new MemoryStream();
        await extractedMap.SaveAsync(memoryStream, new JpegEncoder());
        memoryStream.Position = 0;

        return memoryStream;
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
                            textSize.Height + 8
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
    /// Resolve overlapping points by adjusting their positions
    /// </summary>
    private List<AdjustedPoint> ResolveOverlappingPoints(List<ScumCoordinate> points, float minDistance = 8f)
    {
        var adjustedPoints = new List<AdjustedPoint>();

        // Convert all points to AdjustedPoint objects
        foreach (var point in points)
        {
            var pixelPos = GameCoordinateToPixel(point);
            adjustedPoints.Add(new AdjustedPoint
            {
                OriginalCoordinate = point,
                Color = point.Color,
                Label = point.Label,
                PixelPosition = new PointF(pixelPos.X, pixelPos.Y)
            });
        }

        // Sort by priority (points with labels first, then by color importance)
        adjustedPoints = adjustedPoints.OrderBy(p => string.IsNullOrEmpty(p.Label) ? 1 : 0)
                                       .ThenBy(p => GetColorPriority(p.Color))
                                       .ToList();

        // Resolve overlaps using force-based positioning
        for (int i = 0; i < adjustedPoints.Count; i++)
        {
            var currentPoint = adjustedPoints[i];
            var adjustedPosition = ResolvePointPosition(currentPoint.PixelPosition, adjustedPoints.Take(i).ToList(), minDistance);
            currentPoint.PixelPosition = adjustedPosition;
        }

        return adjustedPoints;
    }

    /// <summary>
    /// Resolves position for a single point to avoid overlaps with existing points
    /// </summary>
    private PointF ResolvePointPosition(PointF originalPos, List<AdjustedPoint> existingPoints, float minDistance)
    {
        if (!existingPoints.Any())
            return originalPos;

        var currentPos = originalPos;
        const int maxAttempts = 50;
        const float stepSize = 2f;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            bool hasOverlap = false;

            foreach (var existingPoint in existingPoints)
            {
                float distance = CalculateDistance(currentPos, existingPoint.PixelPosition);
                if (distance < minDistance)
                {
                    hasOverlap = true;

                    // Calculate push direction (away from overlapping point)
                    var pushDirection = GetPushDirection(currentPos, existingPoint.PixelPosition, minDistance);
                    currentPos = new PointF(
                        currentPos.X + pushDirection.X,
                        currentPos.Y + pushDirection.Y
                    );
                    break;
                }
            }

            if (!hasOverlap)
                break;
        }

        return currentPos;
    }

    /// <summary>
    /// Alternative spiral-based overlap resolution
    /// </summary>
    private PointF ResolvePointPositionSpiral(PointF originalPos, List<AdjustedPoint> existingPoints, float minDistance)
    {
        if (!existingPoints.Any())
            return originalPos;

        // Check if original position is free
        if (!HasOverlapAtPosition(originalPos, existingPoints, minDistance))
            return originalPos;

        // Try positions in expanding spiral pattern
        const float angleStep = (float)(Math.PI / 6); // 30 degrees
        const float radiusStep = 3f;

        for (float radius = minDistance; radius <= minDistance * 4; radius += radiusStep)
        {
            for (float angle = 0; angle < 2 * Math.PI; angle += angleStep)
            {
                var testPos = new PointF(
                    originalPos.X + radius * (float)Math.Cos(angle),
                    originalPos.Y + radius * (float)Math.Sin(angle)
                );

                if (!HasOverlapAtPosition(testPos, existingPoints, minDistance))
                    return testPos;
            }
        }

        // Fallback to force-based if spiral fails
        return ResolvePointPosition(originalPos, existingPoints, minDistance);
    }

    /// <summary>
    /// Check if a position has overlaps with existing points
    /// </summary>
    private bool HasOverlapAtPosition(PointF position, List<AdjustedPoint> existingPoints, float minDistance)
    {
        return existingPoints.Any(existing =>
            CalculateDistance(position, existing.PixelPosition) < minDistance);
    }

    /// <summary>
    /// Calculate direction to push overlapping point
    /// </summary>
    private PointF GetPushDirection(PointF currentPos, PointF blockingPos, float minDistance)
    {
        float deltaX = currentPos.X - blockingPos.X;
        float deltaY = currentPos.Y - blockingPos.Y;

        // If points are exactly on top of each other, pick random direction
        if (Math.Abs(deltaX) < 0.1f && Math.Abs(deltaY) < 0.1f)
        {
            var random = new Random();
            double angle = random.NextDouble() * 2 * Math.PI;
            deltaX = (float)Math.Cos(angle);
            deltaY = (float)Math.Sin(angle);
        }

        // Normalize direction
        float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        if (distance > 0)
        {
            deltaX /= distance;
            deltaY /= distance;
        }

        // Scale to minimum distance
        float pushStrength = minDistance - distance + 1f;
        return new PointF(deltaX * pushStrength, deltaY * pushStrength);
    }

    /// <summary>
    /// Assign priority to colors for overlap resolution (lower = higher priority)
    /// </summary>
    private int GetColorPriority(Color color)
    {
        if (color == Color.Red) return 0;      // Highest priority
        if (color == Color.Orange) return 1;
        if (color == Color.Yellow) return 2;
        if (color == Color.Green) return 3;
        if (color == Color.Blue) return 4;
        if (color == Color.Purple) return 5;
        return 6; // Default/other colors
    }

    /// <summary>
    /// Enhanced method to draw points with overlap resolution
    /// </summary>
    private void DrawPointsOnOriginalMapWithOverlapResolution(Image<Rgba32> originalMap, List<ScumCoordinate> points, bool useSpiral = true)
    {
        // Resolve overlapping positions
        var adjustedPoints = useSpiral ?
            ResolveOverlappingPointsSpiral(points) :
            ResolveOverlappingPoints(points);

        originalMap.Mutate(ctx =>
        {
            // Draw connection lines from original to adjusted positions (optional, for debugging)
            foreach (var pointData in adjustedPoints)
            {
                var originalPixel = GameCoordinateToPixel(pointData.OriginalCoordinate);
                var adjustedPixel = pointData.PixelPosition;

                float distance = CalculateDistance(new PointF(originalPixel.X, originalPixel.Y), adjustedPixel);

                // Draw subtle connection line if point was moved significantly
                if (distance > 5f)
                {
                    ctx.DrawLine(Color.FromRgba(128, 128, 128, 100), 1f,
                        new PointF(originalPixel.X, originalPixel.Y),
                        adjustedPixel);
                }
            }

            // Draw adjusted points
            foreach (var pointData in adjustedPoints)
            {
                var pos = pointData.PixelPosition;

                // White border
                ctx.Fill(Color.White, new EllipsePolygon(pos.X, pos.Y, 4f));
                // Colored center
                ctx.Fill(pointData.Color, new EllipsePolygon(pos.X, pos.Y, 3f));

                // Draw label if exists
                if (!string.IsNullOrEmpty(pointData.Label))
                {
                    var font = SystemFonts.CreateFont("Arial", 10, FontStyle.Regular);
                    var textSize = TextMeasurer.MeasureSize(pointData.Label, new TextOptions(font));

                    // Position label to avoid further overlaps
                    var labelPos = CalculateLabelPosition(pos, textSize, adjustedPoints);

                    var textRect = new RectangleF(labelPos.X - 2, labelPos.Y - 1, textSize.Width + 4, textSize.Height + 2);
                    ctx.Fill(Color.FromRgba(0, 0, 0, 180), textRect);
                    ctx.DrawText(pointData.Label, font, Color.White, labelPos);
                }
            }
        });
    }

    /// <summary>
    /// Spiral-based overlap resolution (alternative approach)
    /// </summary>
    private List<AdjustedPoint> ResolveOverlappingPointsSpiral(List<ScumCoordinate> points, float minDistance = 8f)
    {
        var adjustedPoints = new List<AdjustedPoint>();

        // Sort by priority
        var sortedPoints = points.OrderBy(p => string.IsNullOrEmpty(p.Label) ? 1 : 0)
                               .ThenBy(p => GetColorPriority(p.Color))
                               .ToList();

        foreach (var point in sortedPoints)
        {
            var originalPixel = GameCoordinateToPixel(point);
            var adjustedPos = ResolvePointPositionSpiral(
                new PointF(originalPixel.X, originalPixel.Y),
                adjustedPoints,
                minDistance);

            adjustedPoints.Add(new AdjustedPoint
            {
                OriginalCoordinate = point,
                Color = point.Color,
                Label = point.Label,
                PixelPosition = adjustedPos
            });
        }

        return adjustedPoints;
    }

    /// <summary>
    /// Calculate optimal label position to avoid overlaps
    /// </summary>
    private PointF CalculateLabelPosition(PointF pointPos, FontRectangle textSize, List<AdjustedPoint> allPoints)
    {
        // Try positions around the point (right, top-right, top, etc.)
        var offsets = new[]
        {
        new PointF(8, -4),      // Right
        new PointF(8, -textSize.Height - 2), // Top-right
        new PointF(-4, -textSize.Height - 4), // Top
        new PointF(-textSize.Width - 4, -textSize.Height - 2), // Top-left
        new PointF(-textSize.Width - 4, -4), // Left
        new PointF(-textSize.Width - 4, 8), // Bottom-left
        new PointF(-4, 8),      // Bottom
        new PointF(8, 8)        // Bottom-right
    };

        foreach (var offset in offsets)
        {
            var testPos = new PointF(pointPos.X + offset.X, pointPos.Y + offset.Y);
            var testRect = new RectangleF(testPos.X, testPos.Y, textSize.Width, textSize.Height);

            // Check if this position overlaps with other points
            bool hasOverlap = allPoints.Any(p =>
            {
                float distance = CalculateDistance(testPos, p.PixelPosition);
                return distance < 15f; // Minimum distance from points to labels
            });

            if (!hasOverlap)
                return testPos;
        }

        // Fallback to default position if no good position found
        return new PointF(pointPos.X + 8, pointPos.Y - 4);
    }

    /// <summary>
    /// Updated DrawPointsOnOriginalMap method with overlap resolution
    /// Replace your existing DrawPointsOnOriginalMap method with this one
    /// </summary>
    private void DrawPointsOnOriginalMap(Image<Rgba32> originalMap, List<ScumCoordinate> points)
    {
        // Use the new overlap resolution method
        DrawPointsOnOriginalMapWithOverlapResolution(originalMap, points, useSpiral: true);
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
    /// Desenha todos os pontos no mapa original
    /// </summary>
    //private void DrawPointsOnOriginalMap(Image<Rgba32> originalMap, List<ScumCoordinate> points)
    //{
    //    // Resolve sobreposições antes de desenhar

    //    originalMap.Mutate(ctx =>
    //    {
    //        // Desenha pontos
    //        foreach (var pointData in points)
    //        {
    //            var pointPixel = GameCoordinateToPixel(pointData);

    //            ctx.Fill(Color.White, new EllipsePolygon(pointPixel.X, pointPixel.Y, 4f));
    //            ctx.Fill(pointData.Color, new EllipsePolygon(pointPixel.X, pointPixel.Y, 3f));

    //            if (!string.IsNullOrEmpty(pointData.Label))
    //            {
    //                // Label
    //                var font = SystemFonts.CreateFont("Arial", 10, FontStyle.Regular);
    //                // Fundo semi-transparente para o texto
    //                var textSize = TextMeasurer.MeasureSize(pointData.Label, new TextOptions(font));
    //                var textRect = new RectangleF(pointPixel.X + 15, pointPixel.Y - 10, textSize.Width + 4, textSize.Height + 2);
    //                ctx.Fill(Color.FromRgba(0, 0, 0, 180), textRect);
    //                ctx.DrawText(pointData.Label, font, Color.White, new PointF(pointPixel.X + 17, pointPixel.Y - 8));

    //            }
    //        }
    //    });
    //}

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

    /// <summary>
    /// Calcula se a posição passada está dentro da area do cubo a partir do centro
    /// </summary>
    public bool IsInsideCube(ScumCoordinate target, float centerToVertex)
    {
        float halfSide = centerToVertex / (float)Math.Sqrt(3);

        return Math.Abs(target.X - X) <= halfSide &&
               Math.Abs(target.Y - Y) <= halfSide &&
               Math.Abs(target.Z - Z) <= halfSide;
    }
    public override readonly string ToString() => $"X={X} Y={Y} Z={Z}".Replace(",", ".");

    public static ScumCoordinate MidPoint((double x1, double y1) point1, (double x2, double y2) point2)
    {
        double midX = (point1.x1 + point2.x2) / 2;
        double midY = (point1.y1 + point2.y2) / 2;
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