using System.Threading;

// Namespace "Requests" (không phải "Request"): code đầu phun ghép từ Dirit có class tên Request,
// một namespace PlumbingSolution.Request sẽ che mất class đó (lỗi CS0118).
namespace PlumbingSolution.Requests
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
