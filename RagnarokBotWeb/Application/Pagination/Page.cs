namespace RagnarokBotWeb.Application.Pagination
{
    public class Page<T>
    {
        public Page(IEnumerable<T> content, int totalPages, int totalElements, int number, int size)
        {
            Content = content;
            TotalPages = totalPages;
            TotalElements = totalElements;
            Number = number;
            Size = size;
        }

        public int Number { get; set; }
        public int Size { get; set; }
        public IEnumerable<T> Content { get; set; }
        public int TotalPages { get; set; }
        public int TotalElements { get; set; }

        public static Page<T> FromIPage(IEnumerable<T> content, int totalPages, int totalElements, int number, int size)
        {
            return new Page<T>(content, totalPages, totalElements, number, size);
        }
    }
}
