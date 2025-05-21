namespace EasySMS.API.Common
{
    public class GenericEqualityComparer<T> : IEqualityComparer<T>
    {
        public GenericEqualityComparer(Func<T?, T?, bool> equalsFunc, Func<T, int> getHashCodeFunc)
        {
            ArgumentNullException.ThrowIfNull(equalsFunc);
            ArgumentNullException.ThrowIfNull(getHashCodeFunc);

            this.equalsFunc = equalsFunc;
            this.getHashCodeFunc = getHashCodeFunc;
        }

        private readonly Func<T?, T?, bool> equalsFunc;
        private readonly Func<T, int> getHashCodeFunc;

        public bool Equals(T? x, T? y)
        {
            return equalsFunc(x, y);
        }

        public int GetHashCode(T obj)
        {
            return getHashCodeFunc(obj);
        }
    }
}
