using RagnarokBotWeb.Domain.Entities;

namespace RagnarokBotWeb.Domain.Business
{
    public static class WarzoneItemSelector
    {
        private static readonly Random _random = new();

        public static WarzoneItem SelectItem(Warzone warzone)
        {
            var validItems = warzone.WarzoneItems.Where(i => i.Deleted == null).ToList();

            int totalWeight = validItems.Sum(i => i.Priority);

            int roll = _random.Next(1, totalWeight + 1);

            int current = 0;
            foreach (var item in validItems)
            {
                current += item.Priority;
                if (roll <= current)
                {
                    return item;
                }
            }

            throw new InvalidOperationException("Weighted selection failed. Check item list.");
        }

        public static WarzoneSpawn SelectSpawnPoint(Warzone warzone)
        {
            int r = _random.Next(warzone.SpawnPoints.Count);
            return warzone.SpawnPoints[r];

        }
    }
}
