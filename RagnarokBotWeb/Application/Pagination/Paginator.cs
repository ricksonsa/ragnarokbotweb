namespace RagnarokBotWeb.Application.Pagination
{
    public class Paginator
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public Paginator() { }

        public Paginator(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        public static Paginator Default => new(1, 10);
    }
}
