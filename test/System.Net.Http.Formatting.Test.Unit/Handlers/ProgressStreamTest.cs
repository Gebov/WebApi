﻿// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Moq;
using Xunit;

namespace System.Net.Http.Handlers
{
    public class ProgressStreamTest
    {
        [Fact]
        public void Read_ReportsBytesRead()
        {
            // Arrange
            HttpResponseMessage response = CreateResponse();
            Stream innerStream = response.Content.ReadAsStreamAsync().Result;
            long? expectedLength = response.Content.Headers.ContentLength;
            MockProgressEventHandler mockProgressEventHandler;
            ProgressMessageHandler progressMessageHandler = MockProgressEventHandler.CreateProgressMessageHandler(out mockProgressEventHandler, sendProgress: false);
            ProgressStream progressStream = CreateProgressStream(innerStream: innerStream, progressMessageHandler: progressMessageHandler, response: response);

            // Act/Assert
            int totalBytesRead = 0;
            int bytesRead = 0;
            do
            {
                byte[] buffer = new byte[8];
                bytesRead = progressStream.Read(buffer, 0, buffer.Length);
                totalBytesRead += bytesRead;

                Assert.Equal(totalBytesRead, mockProgressEventHandler.EventArgs.BytesTransferred);
                Assert.Equal((100L * totalBytesRead) / expectedLength, mockProgressEventHandler.EventArgs.ProgressPercentage);
            }
            while (bytesRead > 0);

            Assert.Equal(expectedLength, mockProgressEventHandler.EventArgs.TotalBytes);
            Assert.Equal(100, mockProgressEventHandler.EventArgs.ProgressPercentage);
        }

        [Fact]
        public void ReadByte_ReportsBytesRead()
        {
            // Arrange
            HttpResponseMessage response = CreateResponse();
            Stream innerStream = response.Content.ReadAsStreamAsync().Result;
            long? expectedLength = response.Content.Headers.ContentLength;
            MockProgressEventHandler mockProgressEventHandler;
            ProgressMessageHandler progressMessageHandler = MockProgressEventHandler.CreateProgressMessageHandler(out mockProgressEventHandler, sendProgress: false);
            ProgressStream progressStream = CreateProgressStream(innerStream: innerStream, progressMessageHandler: progressMessageHandler, response: response);

            // Act/Assert
            int totalBytesRead = 0;
            while (progressStream.ReadByte() != -1)
            {
                totalBytesRead += 1;

                Assert.Equal(totalBytesRead, mockProgressEventHandler.EventArgs.BytesTransferred);
                Assert.Equal((100L * totalBytesRead) / expectedLength, mockProgressEventHandler.EventArgs.ProgressPercentage);
            }

            Assert.Equal(expectedLength, mockProgressEventHandler.EventArgs.TotalBytes);
            Assert.Equal(100, mockProgressEventHandler.EventArgs.ProgressPercentage);
        }

        [Fact]
        public void BeginEndRead_ReportsBytesRead()
        {
            // Arrange
            HttpResponseMessage response = CreateResponse();
            Stream innerStream = response.Content.ReadAsStreamAsync().Result;
            long? expectedLength = response.Content.Headers.ContentLength;
            MockProgressEventHandler mockProgressEventHandler;
            ProgressMessageHandler progressMessageHandler = MockProgressEventHandler.CreateProgressMessageHandler(out mockProgressEventHandler, sendProgress: false);
            ProgressStream progressStream = CreateProgressStream(innerStream: innerStream, progressMessageHandler: progressMessageHandler, response: response);
            object userState = new object();

            // Act/Assert
            int totalBytesRead = 0;
            int bytesRead = 0;
            do
            {
                byte[] buffer = new byte[8];
                IAsyncResult result = progressStream.BeginRead(buffer, 0, buffer.Length, null, userState);
                bytesRead = progressStream.EndRead(result);
                totalBytesRead += bytesRead;

                Assert.Same(userState, mockProgressEventHandler.EventArgs.UserState);
                Assert.Equal(totalBytesRead, mockProgressEventHandler.EventArgs.BytesTransferred);
                Assert.Equal((100L * totalBytesRead) / expectedLength, mockProgressEventHandler.EventArgs.ProgressPercentage);
            }
            while (bytesRead > 0);

            Assert.Equal(expectedLength, mockProgressEventHandler.EventArgs.TotalBytes);
            Assert.Equal(100, mockProgressEventHandler.EventArgs.ProgressPercentage);
        }

        [Fact]
        public void Write_ReportsBytesWritten()
        {
            // Arrange
            HttpRequestMessage request = CreateRequest();
            Stream innerStream = new MemoryStream();
            byte[] buffer = CreateBufferContent();
            long? expectedLength = request.Content.Headers.ContentLength;
            MockProgressEventHandler mockProgressEventHandler;
            ProgressMessageHandler progressMessageHandler = MockProgressEventHandler.CreateProgressMessageHandler(out mockProgressEventHandler, sendProgress: true);
            ProgressStream progressStream = CreateProgressStream(innerStream: innerStream, progressMessageHandler: progressMessageHandler, request: request);

            // Act/Assert
            int totalBytesWritten = 0;
            int bytesWritten = 0;
            while (totalBytesWritten < expectedLength)
            {
                bytesWritten = Math.Min(8, (int)expectedLength - totalBytesWritten);
                progressStream.Write(buffer, totalBytesWritten, bytesWritten);
                totalBytesWritten += bytesWritten;

                Assert.Equal(totalBytesWritten, mockProgressEventHandler.EventArgs.BytesTransferred);
                Assert.Equal((100L * totalBytesWritten) / expectedLength, mockProgressEventHandler.EventArgs.ProgressPercentage);
            }

            Assert.Equal(expectedLength, mockProgressEventHandler.EventArgs.TotalBytes);
            Assert.Equal(100, mockProgressEventHandler.EventArgs.ProgressPercentage);
        }

        [Fact]
        public void WriteByte_ReportsBytesWritten()
        {
            // Arrange
            HttpRequestMessage request = CreateRequest();
            Stream innerStream = new MemoryStream();
            byte[] buffer = CreateBufferContent();
            long? expectedLength = request.Content.Headers.ContentLength;
            MockProgressEventHandler mockProgressEventHandler;
            ProgressMessageHandler progressMessageHandler = MockProgressEventHandler.CreateProgressMessageHandler(out mockProgressEventHandler, sendProgress: true);
            ProgressStream progressStream = CreateProgressStream(innerStream: innerStream, progressMessageHandler: progressMessageHandler, request: request);

            // Act/Assert
            int totalBytesWritten = 0;
            while (totalBytesWritten < expectedLength)
            {
                progressStream.WriteByte(buffer[totalBytesWritten]);
                totalBytesWritten += 1;

                Assert.Equal(totalBytesWritten, mockProgressEventHandler.EventArgs.BytesTransferred);
                Assert.Equal((100L * totalBytesWritten) / expectedLength, mockProgressEventHandler.EventArgs.ProgressPercentage);
            }

            Assert.Equal(expectedLength, mockProgressEventHandler.EventArgs.TotalBytes);
            Assert.Equal(100, mockProgressEventHandler.EventArgs.ProgressPercentage);
        }

        [Fact(Timeout = 5000)]
        public void BeginEndWrite_ReportsBytesWritten()
        {
            // Arrange
            HttpRequestMessage request = CreateRequest();
            Stream innerStream = new MemoryStream();
            byte[] buffer = CreateBufferContent();
            long? expectedLength = request.Content.Headers.ContentLength;
            MockProgressEventHandler mockProgressEventHandler;
            ProgressMessageHandler progressMessageHandler = MockProgressEventHandler.CreateProgressMessageHandler(out mockProgressEventHandler, sendProgress: true);
            ProgressStream progressStream = CreateProgressStream(innerStream: innerStream, progressMessageHandler: progressMessageHandler, request: request);
            object userState = new object();

            // Act/Assert
            int totalBytesWritten = 0;
            int bytesWritten = 0;
            while (totalBytesWritten < expectedLength)
            {
                bytesWritten = Math.Min(8, (int)expectedLength - totalBytesWritten);
                IAsyncResult result = progressStream.BeginWrite(buffer, totalBytesWritten, bytesWritten, null, userState);
                progressStream.EndWrite(result);
                totalBytesWritten += bytesWritten;

                Assert.Same(userState, mockProgressEventHandler.EventArgs.UserState);
                Assert.Equal(totalBytesWritten, mockProgressEventHandler.EventArgs.BytesTransferred);
                Assert.Equal((100L * totalBytesWritten) / expectedLength, mockProgressEventHandler.EventArgs.ProgressPercentage);
            }

            Assert.Equal(expectedLength, mockProgressEventHandler.EventArgs.TotalBytes);
            Assert.Equal(100, mockProgressEventHandler.EventArgs.ProgressPercentage);
        }

        internal static ProgressStream CreateProgressStream(
            Stream innerStream = null,
            ProgressMessageHandler progressMessageHandler = null,
            HttpRequestMessage request = null,
            HttpResponseMessage response = null)
        {
            Stream iStream = innerStream ?? new Mock<Stream>().Object;
            ProgressMessageHandler pHandler = progressMessageHandler ?? new ProgressMessageHandler();
            HttpRequestMessage req = request ?? new HttpRequestMessage();
            HttpResponseMessage rsp = response ?? new HttpResponseMessage();
            return new ProgressStream(iStream, pHandler, req, rsp);
        }

        private static HttpRequestMessage CreateRequest()
        {
            return new HttpRequestMessage
            {
                Content = CreateStringContent()
            };
        }

        private static HttpResponseMessage CreateResponse()
        {
            return new HttpResponseMessage
            {
                Content = CreateStringContent()
            };
        }

        private static String CreateContent()
        {
            StringBuilder content = new StringBuilder();
            for (int count = 0; count < 100; count++)
            {
                content.Append("1234567890");
            }
            return content.ToString();
        }

        private static HttpContent CreateStringContent()
        {
            return new StringContent(CreateContent());
        }

        private static byte[] CreateBufferContent()
        {
            return Encoding.UTF8.GetBytes(CreateContent());
        }
    }
}