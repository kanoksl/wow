﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ChatClassLibrary;

namespace ChatClientWPF
{
    public partial class ChatWindow : Window
    {
        /// <summary>
        /// The ID of the chatroom in case of group chat, or the ID of other
        /// client in case of private chat.
        /// </summary>
        public Guid ChatId { get; set; }

        private string chatName;

        public string ChatName
        {
            get { return chatName; }
            set
            {
                chatName = value;
                this.Title = "Chat# - " + chatName
                    + (PrivateMode ? " [Private Chat]" : " [Group Chat]");
            }
        }

        public bool PrivateMode { get; set; }

        public MessageClient ClientService { get; set; }

        private ObservableCollection<MessageLine> ChatHistory { get; set; }

        public ChatWindow(Guid roomId, MessageClient clientService, bool privateChat = false)
        {
            InitializeComponent();

            this.ChatId = roomId;
            this.ClientService = clientService;
            this.PrivateMode = privateChat;

            this.ChatHistory = new ObservableCollection<MessageLine>();
            listView_Chat.ItemsSource = ChatHistory;

            tab_More.Visibility = privateChat ? Visibility.Collapsed : Visibility.Visible;
        }

        public void LoadMemberList()
        {
            if (this.PrivateMode)
                return;

            try
            {
                listBox_Members.ItemsSource = null;
                listBox_Members.ItemsSource = ClientService.KnownChatrooms[this.ChatId].MembersInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        public void DisplayMessage(Message message)
        {
            if (message.Text == null || message.Text == "")
                return;

            Guid senderId = message.SenderId;
            string senderName = "-";

            if (senderId == Message.NullID || message.Type == MessageType.Control)
                senderName = "<SERVER>";
            else if (senderId == ClientService.ClientId)
                senderName = "<YOU>";
            else
                senderName = ClientService.GetClientName(senderId);

            this.Dispatcher.Invoke(() =>
            {   // Called from different thread.
                ChatHistory.Add(new MessageLine(message.TimeSent, senderName, message.Text));

                listView_Chat.SelectedIndex = ChatHistory.Count - 1;
                listView_Chat.ScrollIntoView(listView_Chat.SelectedItem);
            });
        }

        private class MessageLine
        {
            public DateTime Time { get; set; }
            public string Sender { get; set; }
            public string MessageText { get; set; }
            public MessageLine(DateTime time, string sender, string text)
            {
                this.Time = time;
                this.Sender = sender;
                this.MessageText = text;
            }

            public string Color
                => Sender == "<SERVER>" ? "Red" 
                   : Sender == "<YOU>" ? "Blue" 
                   : "Black";
              
        }

        private void btnChatSend_Click(object sender, RoutedEventArgs e)
        {
            string messageText = tbxChatInput.Text.Trim();
            ClientService?.SendMessage(messageText, this.ChatId, this.PrivateMode);

            tbxChatInput.Text = "";
            tbxChatInput.Focus();
        }

        private void tbxChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnChatSend_Click(sender, e);
                e.Handled = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        private void btnLeaveChatroom_Click(object sender, RoutedEventArgs e)
        {
            ClientService?.RequestLeaveChatroom(this.ChatId);
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (((TabControl) sender).SelectedIndex == 2)
            //{
            //    this.LoadMemberList();
            //}
        }

        private void File_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[]) e.Data.GetData(DataFormats.FileDrop);
                tbxFilePath.Text = files[0];
            }
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = "Choose a file to upload";
            dialog.Filter = "All Files (*.*)|*.*";
            dialog.CheckFileExists = true;
            dialog.InitialDirectory = @"D:\";

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                tbxFilePath.Text = dialog.FileName;
            }
        }

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            var progressReporter = new Progress<double>();
            progressReporter.ProgressChanged += (s, progress) =>
            {
                pgbFileUpload.Value = (int) progress;
            };

            string filePath = tbxFilePath.Text.Trim();
            IPEndPoint serverEP = new IPEndPoint(
                ClientService.ServerEndPoint.Address,
                FileProtocol.FtpListeningPort);

            Thread ftpThread = new Thread(() =>
            {
                bool success = FileProtocol.SendFileExtended(filePath, serverEP, 
                    ClientService.ClientId, this.ChatId, progressReporter);
                //Message result = new Message
                //{
                //    Type = MessageType.SystemMessage,
                //    ControlInfo = ControlInfo.None,
                //    TimeSent = DateTime.Now,
                //    Text = success ? "<file upload completed>" : "<FILE UPLOAD FAILED>"
                //};
                //DisplayMessage(result);
                MessageBox.Show("The file has finished uploading.", "File Upload", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ftpThread.Name = "FTP Upload Thread";
            ftpThread.Start();
        }
    }
}
