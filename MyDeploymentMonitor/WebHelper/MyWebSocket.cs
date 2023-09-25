using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyDeploymentMonitor.WebHelper
{
    public class MyWebSocket
    {
        public ClientWebSocket WebSocket { get; private set; } = new ClientWebSocket();
        CancellationToken cancellation = new CancellationToken();
        public string WsConnectPath { get; set; }

        byte[] receiveBytes = new byte[1024*5];
        ArraySegment<byte> receiveArray;

        public MyWebSocket(string wsConnectPath)
        {
            receiveArray = new ArraySegment<byte>(receiveBytes);
            WsConnectPath = wsConnectPath;
            WebSocket.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
            //System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        }

        public async Task OpenAsync(int retryTime=0)
        {
            if(WebSocket==null)
            {
                WebSocket = new ClientWebSocket();
            }
            //WebSocket.Options.SetRequestHeader("Authorization", string.Format("Bearer {0}", "token-bj4lb:lnwsfjqrx9q5bps8fsrtmmttqw44chbqnqrrd8srzmrrfv7vl22ljb"));
            bool reTry = true;
            Exception nowException = null;
            while (reTry)
            {
                try
                {
                    await WebSocket.ConnectAsync(new Uri(WsConnectPath), new CancellationTokenSource(10000).Token);
                }
                catch (Exception ex)
                {
                    retryTime = retryTime - 1;
                    if(retryTime<0)
                    {
                        nowException = ex;
                        break;
                    }
                    await Task.Delay(2000);
                    await this.Close();
                    continue;
                }
                reTry = false;
            }
            if (nowException != null) throw (nowException);
        }

        public async Task SendAsync(string mes)
        {
            if (WebSocket.State == WebSocketState.Open)
            {
                ArraySegment<byte> array = new ArraySegment<byte>(Encoding.UTF8.GetBytes(mes));

                await WebSocket.SendAsync(array, WebSocketMessageType.Text, true, cancellation);
            }
        }

        public async Task Close()
        {
            try
            {
                if (WebSocket != null && WebSocket.State != WebSocketState.Closed)
                {
                    await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "close by by", cancellation);
                }
            }
            catch
            {
                WebSocket.Dispose();
            }
            finally
            {
                WebSocket = null;
            }
        }


        public async Task<string> ReceiveMesAsync(int time = 5)
        {
            int waitTime = time;
            StringBuilder stringBuilder = new StringBuilder();
            CancellationTokenSource source = new CancellationTokenSource(60000);
            CancellationToken token = source.Token;
            Action CancelToken = () =>
            {
                while (waitTime > 0)
                {
                    Task.Delay(1000).Wait();
                    waitTime--;
                }
                if (this.WebSocket != null)
                {
                    Close().Wait();
                }
            };
            _ = Task.Run(CancelToken);
            while (WebSocket.State == WebSocketState.Open)
            {
                if(token.IsCancellationRequested)
                {
                    break;
                }
                try
                {
                    var result = await WebSocket.ReceiveAsync(receiveArray, token);

                    if (result.MessageType == WebSocketMessageType.Text || result.Count > 0)
                    {
                        waitTime = time;
                        stringBuilder.Append(Encoding.UTF8.GetString(receiveArray.ToArray(), 0, result.Count));
                        //System.Diagnostics.Debug.WriteLine(Encoding.UTF8.GetString(receiveArray.ToArray(), 0, result.Count));
                        if (result.EndOfMessage)
                        {
                            continue;
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("主动取消 CancelToken is cancle");
                    break;
                }

            }
            return stringBuilder.ToString();
        }

    }
}
