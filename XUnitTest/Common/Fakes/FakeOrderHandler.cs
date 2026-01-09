using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XUnitTest.Common.Fakes
{
    public sealed class FakeOrderHandler : HttpMessageHandler
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.Created;
        public Guid? FixedOrderId { get; set; }
        public string? Error { get; set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequestBody = request.Content != null ? await request.Content.ReadAsStringAsync(ct) : null;

            HttpResponseMessage resp;

            if ((int)StatusCode >= 200 && (int)StatusCode < 300)
            {
                var id = FixedOrderId ?? Guid.NewGuid();
                var json = $"{{\"id\":\"{id}\"}}";

                resp = new HttpResponseMessage(StatusCode)
                {
                    Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
                };
            }
            else
            {
                resp = new HttpResponseMessage(StatusCode)
                {
                    Content = new StringContent(Error ?? "bad", System.Text.Encoding.UTF8, "text/plain")
                };
            }

            // 👇 Quan trọng: gắn lại RequestMessage để Refit không NRE
            resp.RequestMessage = request;

            return resp;
        }
    }



}
