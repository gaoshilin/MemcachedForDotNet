using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Enyim.Caching.Memcached
{
	[DebuggerDisplay("[ Address: {endpoint}, IsAlive = {IsAlive} ]")]
	internal class PooledSocket : IDisposable
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(PooledSocket));

		private const int ErrorResponseLength = 13;

		private const string GenericErrorResponse = "ERROR";
		private const string ClientErrorResponse = "CLIENT_ERROR ";
		private const string ServerErrorResponse = "SERVER_ERROR ";

		private bool isAlive = true;
		private Socket socket;
		private Action<PooledSocket> cleanupCallback;
		private readonly IPEndPoint endpoint;

		private BufferedStream inputStream;

		internal PooledSocket(IPEndPoint endpoint, TimeSpan connectionTimeout, TimeSpan receiveTimeout, Action<PooledSocket> cleanupCallback)
		{
			this.endpoint = endpoint;
			this.cleanupCallback = cleanupCallback;

			socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, connectionTimeout == TimeSpan.MaxValue ? Timeout.Infinite : (int)connectionTimeout.TotalMilliseconds);
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, receiveTimeout == TimeSpan.MaxValue ? Timeout.Infinite : (int)receiveTimeout.TotalMilliseconds);

			// all operations are "atomic", we do not send small chunks of data
			socket.NoDelay = true;

			socket.Connect(endpoint);
			inputStream = new BufferedStream(new BasicNetworkStream(socket));
		}

		public void Reset()
		{
			//this.LockToThread();

			// discard any buffered data
			inputStream.Flush();

			int available = socket.Available;

			if (available > 0)
			{
				if (log.IsWarnEnabled)
					log.WarnFormat("Socket bound to {0} has {1} unread data! This is probably a bug in the code. InstanceID was {2}.", socket.RemoteEndPoint, available, InstanceId);

				byte[] data = new byte[available];

				Read(data, 0, available);

				if (log.IsWarnEnabled)
					log.Warn(Encoding.ASCII.GetString(data));
			}

			if (log.IsDebugEnabled)
				log.DebugFormat("Socket {0} was reset", InstanceId);
		}

		//private int threadId = -1;

		//private void LockToThread()
		//{
		//    if (this.threadId > -1)
		//        throw new InvalidOperationException("We are already bound to thread #" + this.threadId);

		//    this.threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
		//}

		//private void CheckThread()
		//{
		//    if (this.threadId != System.Threading.Thread.CurrentThread.ManagedThreadId)
		//        throw new InvalidOperationException(String.Format("Thread id differs: {0} vs {1}", this.threadId, System.Threading.Thread.CurrentThread.ManagedThreadId));
		//}

		/// <summary>
		/// The ID of theis instance. Used by the <see cref="T:MemcachedServer"/> to identify the instance in its inner lists.
		/// </summary>
		public readonly Guid InstanceId = Guid.NewGuid();

		public bool IsAlive
		{
			get { return isAlive; }
		}

		/// <summary>
		/// Releases all resources used by this instance and shuts down the inner <see cref="T:Socket"/>. This instance will not be usable anymore.
		/// </summary>
		/// <remarks>Use the IDisposable.Dispose method if you want to release this instance back into the pool.</remarks>
		public void Destroy()
		{
			Dispose(true);
		}

		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				GC.SuppressFinalize(this);

				if (socket != null)
				{
					using (socket)
						socket.Shutdown(SocketShutdown.Both);
				}

				inputStream.Dispose();

				inputStream = null;
				socket = null;
				cleanupCallback = null;
			}
			else
			{
				Action<PooledSocket> cc = cleanupCallback;

				if (cc != null)
				{
					cc(this);
				}
			}
		}

		void IDisposable.Dispose()
		{
			Dispose(false);
		}

		private void CheckDisposed()
		{
			if (socket == null)
				throw new ObjectDisposedException("PooledSocket");
		}

		/// <summary>
		/// Reads a line from the socket. A line is terninated by \r\n.
		/// </summary>
		/// <returns></returns>
		private string ReadLine()
		{
			MemoryStream ms = new MemoryStream(50);

			bool gotR = false;

		    try
			{
				while (true)
				{
					int data = inputStream.ReadByte();

					if (data == 13)
					{
						gotR = true;
						continue;
					}

					if (gotR)
					{
						if (data == 10)
							break;

						ms.WriteByte(13);

						gotR = false;
					}

					ms.WriteByte((byte)data);
				}
			}
			catch (IOException)
			{
				isAlive = false;

				throw;
			}

			string retval = Encoding.ASCII.GetString(ms.GetBuffer(), 0, (int)ms.Length);

			if (log.IsDebugEnabled)
				log.Debug("ReadLine: " + retval);

			return retval;
		}

		/// <summary>
		/// Sends the command to the server. The trailing \r\n is automatically appended.
		/// </summary>
		/// <param name="value">The command to be sent to the server.</param>
		public void SendCommand(string value)
		{
			CheckDisposed();
			//this.CheckThread();

			if (log.IsDebugEnabled)
				log.Debug("SendCommand: " + value);

			// send the whole command with only one Write
			// since Nagle is disabled on the socket this is more efficient than
			// Write(command), Write("\r\n")
			Write(GetCommandBuffer(value));
		}

		/// <summary>
		/// Gets the bytes representing the specified command. returned buffer can be used to streamline multiple writes into one Write on the Socket
		/// using the <see cref="M:Enyim.Caching.Memcached.PooledSocket.Write(IList&lt;ArraySegment&lt;byte&gt;&gt;)"/>
		/// </summary>
		/// <param name="value">The command to be converted.</param>
		/// <returns>The buffer containing the bytes representing the command. The returned buffer will be terminated with 13, 10 (\r\n)</returns>
		/// <remarks>The Nagle algorithm is disabled on the socket to speed things up, so it's recommended to convert a command into a buffer
		/// and use the <see cref="M:Enyim.Caching.Memcached.PooledSocket.Write(IList&lt;ArraySegment&lt;byte&gt;&gt;)"/> to send the command and the additional buffers in one transaction.</remarks>
		public static ArraySegment<byte> GetCommandBuffer(string value)
		{
			int valueLength = value.Length;
			byte[] data = new byte[valueLength + 2];

			Encoding.ASCII.GetBytes(value, 0, valueLength, data, 0);

			data[valueLength] = 13;
			data[valueLength + 1] = 10;

			return new ArraySegment<byte>(data);
		}

		/// <summary>
		/// Reads data from the server into the specified buffer.
		/// </summary>
		/// <param name="buffer">An array of <see cref="T:System.Byte"/> that is the storage location for the received data.</param>
		/// <param name="offset">The location in buffer to store the received data.</param>
		/// <param name="count">The number of bytes to read.</param>
		/// <remarks>This method blocks and will not return until the specified amount of bytes are read.</remarks>
		public void Read(byte[] buffer, int offset, int count)
		{
			CheckDisposed();
			//this.CheckThread();

			if (log.IsDebugEnabled)
				log.DebugFormat("Reading {0} bytes into buffer starting at {1}", count, offset);

			int read = 0;
			int shouldRead = count;

			while (read < count)
			{
				try
				{
					int currentRead = inputStream.Read(buffer, offset, shouldRead);
					if (currentRead < 1)
						continue;

					read += currentRead;
					offset += currentRead;
					shouldRead -= currentRead;
				}
				catch (IOException)
				{
					isAlive = false;
					throw;
				}
			}
		}

		public void Write(ArraySegment<byte> data)
		{
			Write(data.Array, data.Offset, data.Count);
		}

		public void Write(byte[] data, int offset, int length)
		{
			CheckDisposed();
			//this.CheckThread();

			if (log.IsDebugEnabled)
				log.DebugFormat("Writing {0} bytes from buffer starting at {1}", length, offset);

			SocketError status;

			socket.Send(data, offset, length, SocketFlags.None, out status);

			if (status != SocketError.Success)
			{
				isAlive = false;

				ThrowHelper.ThrowSocketWriteError(endpoint, status);
			}
		}

		public void Write(IList<ArraySegment<byte>> buffers)
		{
			CheckDisposed();
			//this.CheckThread();

			if (log.IsDebugEnabled)
				log.DebugFormat("Writing {0} buffer(s)", buffers.Count);

			SocketError status;

			socket.Send(buffers, SocketFlags.None, out status);

			if (status != SocketError.Success)
			{
				isAlive = false;

				ThrowHelper.ThrowSocketWriteError(endpoint, status);
			}
		}

		/// <summary>
		/// Reads the response of the server.
		/// </summary>
		/// <returns>The data sent by the memcached server.</returns>
		/// <exception cref="T:System.InvalidOperationException">The server did not sent a response or an empty line was returned.</exception>
		/// <exception cref="T:Enyim.Caching.Memcached.MemcachedException">The server did not specified any reason just returned the string ERROR. - or - The server returned a SERVER_ERROR, in this case the Message of the exception is the message returned by the server.</exception>
		/// <exception cref="T:Enyim.Caching.Memcached.MemcachedClientException">The server did not recognize the request sent by the client. The Message of the exception is the message returned by the server.</exception>
		public string ReadResponse()
		{
			CheckDisposed();
			//this.CheckThread();

			string response = ReadLine();

			if (log.IsDebugEnabled)
				log.Debug("Received response: " + response);

			if (String.IsNullOrEmpty(response))
				throw new MemcachedClientException("Empty response received.");

			if (String.Compare(response, GenericErrorResponse, StringComparison.Ordinal) == 0)
				throw new NotSupportedException("Operation is not supported by the server or the request was malformed. If the latter please report the bug to the developers.");

			if (response.Length >= ErrorResponseLength)
			{
			    if (String.Compare(response, 0, ClientErrorResponse, 0, ErrorResponseLength, StringComparison.Ordinal) == 0)
				{
					throw new MemcachedClientException(response.Remove(0, ErrorResponseLength));
				}
			    if (String.Compare(response, 0, ServerErrorResponse, 0, ErrorResponseLength, StringComparison.Ordinal) == 0)
			    {
			        throw new MemcachedException(response.Remove(0, ErrorResponseLength));
			    }
			}

		    return response;
		}

		#region [ BasicNetworkStream           ]
		private class BasicNetworkStream : Stream
		{
			private readonly Socket socket;

			public BasicNetworkStream(Socket socket)
			{
				this.socket = socket;
			}

			public override bool CanRead
			{
				get { return true; }
			}

			public override bool CanSeek
			{
				get { return false; }
			}

			public override bool CanWrite
			{
				get { return false; }
			}

			public override void Flush()
			{
			}

			public override long Length
			{
				get { throw new NotSupportedException(); }
			}

			public override long Position
			{
				get { throw new NotSupportedException(); }
				set { throw new NotSupportedException(); }
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				SocketError errorCode;

				int retval = socket.Receive(buffer, offset, count, SocketFlags.None, out errorCode);

				if (errorCode == SocketError.Success)
					return retval;

				throw new IOException(String.Format("Failed to read from the socket '{0}'. Error: {1}", socket.RemoteEndPoint, errorCode));
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotSupportedException();
			}

			public override void SetLength(long value)
			{
				throw new NotSupportedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
			}
		}
		#endregion
	}
}

#region [ License information          ]
/* ************************************************************
 *
 * Copyright (c) Attila Kiskó, enyim.com, 2007
 *
 * This source code is subject to terms and conditions of 
 * Microsoft Permissive License (Ms-PL).
 * 
 * A copy of the license can be found in the License.html
 * file at the root of this distribution. If you can not 
 * locate the License, please send an email to a@enyim.com
 * 
 * By using this source code in any fashion, you are 
 * agreeing to be bound by the terms of the Microsoft 
 * Permissive License.
 *
 * You must not remove this notice, or any other, from this
 * software.
 *
 * ************************************************************/
#endregion