using Atomex.Entities;

namespace Atomex.Common
{
    public class Result<T>
    {
        public T Value { get; }
        public bool HasError => Error != null;
        public Error Error { get; }

        public Result(T value) => Value = value;
        public Result(Error error) => Error = error;

        public static implicit operator Result<T>(T value) => new Result<T>(value);
        public static implicit operator Result<T>(Error error) => new Result<T>(error);
    }
}