﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ChatClassLibrary
{
    public class MessageEventArgs : EventArgs
    {
        public Message Message { get; private set; }
        public MessageEventArgs(Message message) { this.Message = message; }
        public MessageEventArgs() { }
    }

    public class ConnectionEventArgs : EventArgs
    {
        public IPEndPoint ServerEndPoint { get; private set; }
        public TcpClient ClientSocket { get; private set; }
        public ConnectionEventArgs(IPEndPoint serverEP, TcpClient clientSocket)
        {
            this.ServerEndPoint = serverEP;
            this.ClientSocket = clientSocket;
        }
        public ConnectionEventArgs() { }
    }

    public class ChatroomEventArgs : EventArgs
    {
        public Guid ChatroomId { get; private set; }
        public string ChatroomName { get; private set; }
        public ChatroomEventArgs(Guid chatroomId, string chatroomName)
        {
            this.ChatroomId = chatroomId;
            this.ChatroomName = chatroomName;
        }
    }

    public class MessageClient
    {
        public class ClientInfo
        {
            public Guid ClientId { get; set; }
            public string DisplayName { get; set; }

            public string icon => "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";

            public ClientInfo(Guid id, string name)
            {
                this.ClientId = id;
                this.DisplayName = name;
            }

            public override string ToString()
            {
                return "ClientInfo\n"
                        + "    ClientID: " + this.ClientId.ToString()
                        + "    DisplayName: " + this.DisplayName;
            }
        }

        public class ChatroomInfo
        {
            public Guid ChatroomId { get; set; }
            public string ChatroomName { get; set; }
            public int MemberCount => Members?.Count ?? 0;
            public List<Guid> Members { get; set; }  // Only for joined rooms.
            public List<ClientInfo> MembersInfo { get; set; }

            //public int MemberCount2 { get; set; }

            public ChatroomInfo(Guid id, string name)
            {
                this.ChatroomId = id;
                this.ChatroomName = name;
                this.Members = new List<Guid>();
                this.MembersInfo = new List<ClientInfo>();
            }

            public override string ToString()
            {
                return "ChatroomInfo\n"
                        + "    ChatroomID: " + this.ChatroomId.ToString()
                        + "    ChatroomName: " + this.ChatroomName
                        + "    MemberCount: " + this.MemberCount;
            }
        }

        public string GetChatroomName(Guid chatroomId)
        {
            if (KnownChatrooms.ContainsKey(chatroomId))
                return KnownChatrooms[chatroomId].ChatroomName;
            else
                return "<UNKNOWN ROOM>";
        }

        public string GetClientName(Guid clientId)
        {
            if (KnownClients.ContainsKey(clientId))
                return KnownClients[clientId].DisplayName;
            else
                return "<UNKNOWN CLIENT>";
        }

        //--------------------------------------------------------------------------------------//
        #region Properties

        public IPEndPoint ServerEndPoint { get; set; }
        public TcpClient ClientSocket { get; set; }
        public NetworkStream NetworkStream { get; set; }


        public Guid ClientId { get; private set; }
        public string DisplayName { get; set; }


        public Dictionary<Guid, ClientInfo> KnownClients { get; set; }
        public Dictionary<Guid, ChatroomInfo> KnownChatrooms { get; set; }
        public List<Guid> JoinedChatrooms { get; set; }


        public bool Connected
            => this.ClientSocket?.Connected ?? false;

        #endregion
        //--------------------------------------------------------------------------------------//

        public MessageClient(IPAddress serverIP, int port)
        {
            IPEndPoint serverEP = new IPEndPoint(serverIP, port);
            this.ServerEndPoint = serverEP;
            this.ClientId = Guid.NewGuid();

            this.KnownClients = new Dictionary<Guid, ClientInfo>();
            this.KnownChatrooms = new Dictionary<Guid, ChatroomInfo>();
            this.KnownChatrooms.Add(Guid.Empty, new ChatroomInfo(Guid.Empty, "Public Room"));
            this.JoinedChatrooms = new List<Guid>();
        }

        public MessageClient(IPAddress serverIP)
            : this(serverIP, ChatProtocol.ServerListeningPort) { }


        public async void ConnectToServer()
        {
            await Task.Run(() =>
            {
                try
                {   // Connect to the server's socket.
                    this.ClientSocket = new TcpClient();
                    this.ClientSocket.Connect(this.ServerEndPoint);
                    this.NetworkStream = this.ClientSocket.GetStream();

                    Message request = new Message
                    {
                        Type = MessageType.Control,
                        ControlInfo = ControlInfo.ClientRequestConnection,
                        SenderId = this.ClientId,
                        TargetId = Message.NullID,
                        Text = this.DisplayName
                    };
                    ChatProtocol.SendMessage(request, this.NetworkStream);

                    Message reply = ChatProtocol.ReceiveMessage(this.NetworkStream);
                    if (reply.ControlInfo == ControlInfo.ConnectionAccepted)
                    {
                        this.OnConnectionEstablished(
                            new ConnectionEventArgs(this.ServerEndPoint, this.ClientSocket));
                    }
                    else
                    {
                        Console.WriteLine("Server rejected connection: " + reply.Text);
                        this.OnConnectionFailed(
                            new ConnectionEventArgs(this.ServerEndPoint, this.ClientSocket));
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);  // TODO: better handling
                    this.OnConnectionFailed(new ConnectionEventArgs(this.ServerEndPoint,
                                                                    this.ClientSocket));
                }
            });
        }

        public void SendMessage(string messageText, Guid targetId, bool privateMessage = false)
        {
            Message message = new Message
            {
                Type = privateMessage ? MessageType.UserPrivateMessage
                                      : MessageType.UserGroupMessage,
                ControlInfo = ControlInfo.None,
                SenderId = this.ClientId,
                TargetId = targetId,
                Text = messageText
            };

            try
            {
                ChatProtocol.SendMessage(message, this.NetworkStream);
                this.OnMessageSent(new MessageEventArgs(message));
            }
            catch
            {
                this.OnMessageSendingFailed(new MessageEventArgs(message));
            }
        }

        private CancellationTokenSource _receiveCTS;

        public async void BeginReceive()
        {
            _receiveCTS = new CancellationTokenSource();
            var token = _receiveCTS.Token;
            try
            {
                await Task.Run(() =>
                {   // May cause 'exception unhandled by user code' when debugging.
                    while (this.Connected)
                    {
                        token.ThrowIfCancellationRequested();
                        Message message = ChatProtocol.ReceiveMessage(this.NetworkStream);
                        if (!message.IsValid)  // Null message, mostly occur when connection is terminated.
                            continue;
                        else if (message.ControlInfo == ControlInfo.ClientList)
                            UpdateKnownClientList(message.SenderId, message.Text);
                        else if (message.ControlInfo == ControlInfo.ChatroomList)
                            UpdateKnownChatroomList(message.Text);
                        else if (message.ControlInfo == ControlInfo.ClientJoinedChatroom && message.SenderId == this.ClientId)
                            _JoinedChatRoom(message.TargetId);
                        else if (message.ControlInfo == ControlInfo.ClientLeftChatroom && message.SenderId == this.ClientId)
                            _LeftChatroom(message.TargetId);
                        else if (message.Type == MessageType.UserPrivateMessage)
                            this.OnPrivateMessageReceived(new MessageEventArgs(message));
                        else if (message.ControlInfo == ControlInfo.FileAvailable)
                            this.OnFileAvailable(new MessageEventArgs(message));
                        else if (message.ControlInfo == ControlInfo.FileList)
                            this.OnFileListReceived(new MessageEventArgs(message));
                        else
                            this.OnMessageReceived(new MessageEventArgs(message));
                    }
                });
            }
            catch (OperationCanceledException)
            {
                this.OnMessageReceived(new MessageEventArgs(new Message { Text = "CLIENT DISCONNECTED" }));
            }
            catch
            {
                this.OnMessageReceivingingFailed(new MessageEventArgs(new Message { Text = "RECV FAILED" }));
            }

            this.OnServerDisconnected(new ConnectionEventArgs(this.ServerEndPoint, this.ClientSocket));
        }

        private void _JoinedChatRoom(Guid roomId)
        {
            if (!JoinedChatrooms.Contains(roomId))
            {
                JoinedChatrooms.Add(roomId);
                this.OnClientJoinedChatroom(new ChatroomEventArgs(roomId,
                                    GetChatroomName(roomId)));
            }
            this.OnKnownChatroomsUpdated(EventArgs.Empty);
        }

        private void _LeftChatroom(Guid roomId)
        {
            if (JoinedChatrooms.Contains(roomId))
            {
                JoinedChatrooms.Remove(roomId);
                this.OnClientLeftChatroom(new ChatroomEventArgs(roomId,
                                GetChatroomName(roomId)));
            }
            this.OnKnownChatroomsUpdated(EventArgs.Empty);
        }

        public void RequestJoinChatroom(Guid roomId)
        {
            if (JoinedChatrooms.Contains(roomId))
                return;  // Already joined.

            Message request = new Message
            {
                Type = MessageType.Control,
                ControlInfo = ControlInfo.RequestJoinChatroom,
                SenderId = this.ClientId,
                TargetId = roomId
            };
            ChatProtocol.SendMessage(request, this.NetworkStream);
        }

        public void RequestLeaveChatroom(Guid roomId)
        {
            if (!JoinedChatrooms.Contains(roomId))
                return;  // Not a member.

            Message request = new Message
            {
                Type = MessageType.Control,
                ControlInfo = ControlInfo.RequestLeaveChatroom,
                SenderId = this.ClientId,
                TargetId = roomId
            };
            ChatProtocol.SendMessage(request, this.NetworkStream);
        }

        public void RequestCreateChatroom(string roomName)
        {
            Message request = new Message
            {
                Type = MessageType.Control,
                ControlInfo = ControlInfo.RequestCreateChatroom,
                SenderId = this.ClientId,
                TargetId = Message.NullID,
                Text = roomName
            };
            ChatProtocol.SendMessage(request, this.NetworkStream);
        }

        private void UpdateKnownClientList(Guid roomId, string clientList)
        {
            if (roomId == Guid.Empty)
                this.KnownClients.Clear();

            ChatroomInfo roomInfo = null;
            if (this.KnownChatrooms.ContainsKey(roomId))
            {
                roomInfo = KnownChatrooms[roomId];
                roomInfo.Members.Clear();
                roomInfo.MembersInfo.Clear();
            }

            if (clientList != null)
                using (var reader = new StringReader(clientList))
                {
                    string guidStr;
                    while ((guidStr = reader.ReadLine()) != null)
                    {
                        string name = reader.ReadLine();

                        var clientId = Guid.Parse(guidStr);
                        var newClient = new ClientInfo(clientId, name);

                        if (roomId == Guid.Empty || !KnownClients.ContainsKey(clientId))
                            this.KnownClients.Add(clientId, newClient);
                        roomInfo?.Members.Add(clientId);
                        roomInfo?.MembersInfo.Add(newClient);
                    }
                }
            this.OnKnownClientsUpdated(EventArgs.Empty);
        }

        private void UpdateKnownChatroomList(string chatroomList)
        {
            //this.KnownChatrooms.Clear();
            var temp = new HashSet<Guid>();
            using (var reader = new StringReader(chatroomList))
            {
                string guidStr;
                while ((guidStr = reader.ReadLine()) != null)
                {
                    string name = reader.ReadLine();
                    string countStr = reader.ReadLine();

                    var roomId = Guid.Parse(guidStr);
                    var count = int.Parse(countStr);
                    var newRoom = new ChatroomInfo(roomId, name);

                    temp.Add(roomId);
                    if (!KnownChatrooms.ContainsKey(roomId))
                        this.KnownChatrooms.Add(roomId, newRoom);
                    else if (count != KnownChatrooms[roomId].MemberCount)
                        Console.WriteLine("AAAAAAAAAAAAAAAAAAAAAAAAARRRRRRRRRRRRRRRRGGGGGGGGGHHHHHH");
                }
            }

            List<Guid> ids = new List<Guid>();
            foreach (var roomId in KnownChatrooms.Keys)
                ids.Add(roomId);
            foreach (var roomId in ids)
                if (!temp.Contains(roomId)) KnownChatrooms.Remove(roomId);

            this.OnKnownChatroomsUpdated(EventArgs.Empty);
        }

        public void Disconnect()
        {
            if (this.Connected)
            {
                this.ClientSocket.Close();
                this.ClientSocket = null;
                this.NetworkStream = null;

                this._receiveCTS?.Cancel();

                this.KnownChatrooms.Clear();
                this.KnownClients.Clear();
                this.JoinedChatrooms.Clear();

                this.OnClientDisconnected(new ConnectionEventArgs(this.ServerEndPoint, this.ClientSocket));
            }
        }

        //--------------------------------------------------------------------------------------//
        #region Event Handlers and Events

        public event EventHandler<MessageEventArgs> MessageSent;
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<MessageEventArgs> MessageSendingFailed;
        public event EventHandler<MessageEventArgs> MessageReceivingingFailed;
        public event EventHandler<MessageEventArgs> PrivateMessageReceived;

        public event EventHandler<MessageEventArgs> FileAvailable;
        public event EventHandler<MessageEventArgs> FileListReceived;

        public event EventHandler KnownClientsUpdated;
        public event EventHandler KnownChatroomsUpdated;

        public event EventHandler<ChatroomEventArgs> ClientJoinedChatroom;
        public event EventHandler<ChatroomEventArgs> ClientLeftChatroom;

        public event EventHandler<ConnectionEventArgs> ConnectionEstablished;
        public event EventHandler<ConnectionEventArgs> ConnectionFailed;
        public event EventHandler<ConnectionEventArgs> ClientDisconnected;
        public event EventHandler<ConnectionEventArgs> ServerDisconnected;

        protected virtual void OnMessageSent(MessageEventArgs e)
            => MessageSent?.Invoke(this, e);
        protected virtual void OnMessageReceived(MessageEventArgs e)
            => MessageReceived?.Invoke(this, e);
        protected virtual void OnMessageSendingFailed(MessageEventArgs e)
            => MessageSendingFailed?.Invoke(this, e);
        protected virtual void OnMessageReceivingingFailed(MessageEventArgs e)
            => MessageReceivingingFailed?.Invoke(this, e);
        protected virtual void OnPrivateMessageReceived(MessageEventArgs e)
            => PrivateMessageReceived?.Invoke(this, e);

        protected virtual void OnFileAvailable(MessageEventArgs e)
            => FileAvailable?.Invoke(this, e);
        protected virtual void OnFileListReceived(MessageEventArgs e)
            => FileListReceived?.Invoke(this, e);

        protected virtual void OnKnownClientsUpdated(EventArgs e)
            => KnownClientsUpdated?.Invoke(this, e);
        protected virtual void OnKnownChatroomsUpdated(EventArgs e)
            => KnownChatroomsUpdated?.Invoke(this, e);

        protected virtual void OnClientJoinedChatroom(ChatroomEventArgs e)
            => ClientJoinedChatroom?.Invoke(this, e);
        protected virtual void OnClientLeftChatroom(ChatroomEventArgs e)
            => ClientLeftChatroom?.Invoke(this, e);

        protected virtual void OnConnectionEstablished(ConnectionEventArgs e)
            => ConnectionEstablished?.Invoke(this, e);
        protected virtual void OnConnectionFailed(ConnectionEventArgs e)
            => ConnectionFailed?.Invoke(this, e);
        protected virtual void OnClientDisconnected(ConnectionEventArgs e)
            => ClientDisconnected?.Invoke(this, e);
        protected virtual void OnServerDisconnected(ConnectionEventArgs e)
            => ServerDisconnected?.Invoke(this, e);

        #endregion
        //--------------------------------------------------------------------------------------//

        // more events:
        // client list received
        // chatroom list received // currently included in MessageReceived



        // more methods:
        // join chatroom
        // leave chatroom
    }

}