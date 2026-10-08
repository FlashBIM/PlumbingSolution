using System.Threading;

namespace PlumbingSolution.Request
{
    public enum RequestId
    {
        None = -1,
        // Thêm RequestId cho các lệnh modeless của PlumbingSolution tại đây.
    }

    public sealed class Request
    {
        private int _request = (int)RequestId.None;

        public RequestId Take()
        {
            return (RequestId)Interlocked.Exchange(ref _request, (int)RequestId.None);
        }

        public void Make(RequestId request)
        {
            Interlocked.Exchange(ref _request, (int)request);
        }
    }
}
