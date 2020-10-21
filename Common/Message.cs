using Atomex.Entities;

namespace Atomex.Common
{
    public class Message<T>
    {
        public string Type { get; set; }
        public T Content { get; set; }
        public bool HasError => Error != null;
        public Error Error { get; set; }

        public Message(string type, T content)
        {
            Type = type;
            Content = content;
        }

        public Message(Error error) => Error = error;

        public static implicit operator Message<T>(Error error) => new Message<T>(error);
    }
}