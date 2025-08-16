using RagnarokBotWeb.Domain.Entities;
using RagnarokBotWeb.Domain.Services.Dto;

namespace RagnarokBotWeb.Application.Pagination
{
    public class Page<T>
    {
        private Func<IEnumerable<CustomTaskDto>> value;
        private Page<CustomTask> page;

        public int Number { get; set; }
        public int Size { get; set; }
        public IEnumerable<T> Content { get; set; }
        public int TotalPages { get; set; }
        public int TotalElements { get; set; }

        public Page(IEnumerable<T> content, int totalPages, int totalElements, int number, int size)
        {
            Content = content;
            TotalPages = totalPages;
            TotalElements = totalElements;
            Number = number;
            Size = size;
        }

        public static Page<T> FromIPage(IEnumerable<T> content, int totalPages, int totalElements, int number, int size)
        {
            return new Page<T>(content, totalPages, totalElements, number, size);
        }
    }
}
