﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Linq;
using System.Collections.Generic;

using DwFramework.Core.Extensions;
using DwFramework.Core.Plugins;

namespace DwFramework.WebSocket
{
    public class WebSocketClient
    {
        public class OnConnectEventArgs : EventArgs
        {

        }

        public class OnSendEventArgs : EventArgs
        {
            public byte[] Data { get; init; }
        }

        public class OnReceiveEventargs : EventArgs
        {
            public byte[] Data { get; init; }
        }

        public class OnCloceEventArgs : EventArgs
        {
            public WebSocketCloseStatus? CloseStatus { get; init; }
        }

        public class OnErrorEventArgs : EventArgs
        {
            public Exception Exception { get; init; }
        }

        public event Action<OnConnectEventArgs> OnConnect;
        public event Action<OnCloceEventArgs> OnClose;
        public event Action<OnSendEventArgs> OnSend;
        public event Action<OnReceiveEventargs> OnReceive;
        public event Action<OnErrorEventArgs> OnError;

        private readonly ClientWebSocket _client;
        private int _bufferSize = 4096;
        public int BufferSize
        {
            get => _bufferSize;
            set => _bufferSize = value <= 0 ? 4096 : value;
        }
        public WebSocketState State => _client.State;

        /// <summary>
        /// 构造函数
        /// </summary>
        public WebSocketClient()
        {
            _client = new ClientWebSocket();
        }

        /// <summary>
        /// 建立连接
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="header"></param>
        /// <param name="subProtocal"></param>
        /// <returns></returns>
        public async Task ConnectAsync(string uri, Dictionary<string, string> header = null, List<string> subProtocal = null)
        {
            if (header != null) foreach (var item in header) _client.Options.SetRequestHeader(item.Key, item.Value);
            if (subProtocal != null) foreach (var item in subProtocal) _client.Options.AddSubProtocol(item);
            await _client.ConnectAsync(new Uri(uri), CancellationToken.None).ContinueWith(a =>
            {
                OnConnect?.Invoke(new OnConnectEventArgs() { });
                TaskManager.CreateTask(async () =>
                {
                    var buffer = new byte[_bufferSize];
                    var dataBytes = new List<byte>();
                    WebSocketCloseStatus? closeStates = null;
                    while (true)
                    {
                        try
                        {
                            var result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                            if (result.CloseStatus.HasValue)
                            {
                                closeStates = result.CloseStatus;
                                break;
                            }
                            dataBytes.AddRange(buffer.Take(result.Count));
                            if (!result.EndOfMessage) continue;
                            OnReceive?.Invoke(new OnReceiveEventargs() { Data = dataBytes.ToArray() });
                            dataBytes.Clear();
                        }
                        catch (Exception ex)
                        {
                            OnError?.Invoke(new OnErrorEventArgs() { Exception = ex });
                            continue;
                        }
                    }
                    OnClose?.Invoke(new OnCloceEventArgs() { CloseStatus = closeStates });
                    ClearAllEvent();
                    if (_client.State == WebSocketState.CloseReceived) await CloseAsync();
                });
            });
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public async Task SendAsync(byte[] buffer)
        {
            await _client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully) OnSend?.Invoke(new OnSendEventArgs() { Data = buffer });
                else OnError?.Invoke(new OnErrorEventArgs() { Exception = task.Exception.InnerException });
            });
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <returns></returns>
        public async Task CloseAsync()
        {
            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }

        /// <summary>
        /// 清空事件列表
        /// </summary>
        private void ClearAllEvent()
        {
            if (OnConnect != null)
            {
                var actions = OnConnect.GetInvocationList();
                actions.ForEach(item => OnConnect -= (Action<OnConnectEventArgs>)item);
            }
            if (OnClose != null)
            {
                var actions = OnClose.GetInvocationList();
                actions.ForEach(item => OnClose -= (Action<OnCloceEventArgs>)item);
            }
            if (OnError != null)
            {
                var actions = OnError.GetInvocationList();
                actions.ForEach(item => OnError -= (Action<OnErrorEventArgs>)item);
            }
            if (OnSend != null)
            {
                var actions = OnSend.GetInvocationList();
                actions.ForEach(item => OnSend -= (Action<OnSendEventArgs>)item);
            }
            if (OnReceive != null)
            {
                var actions = OnReceive.GetInvocationList();
                actions.ForEach(item => OnReceive -= (Action<OnReceiveEventargs>)item);
            }
        }
    }
}
