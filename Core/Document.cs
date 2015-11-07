// Nombre: Document.cs
// Fecha:  2011-01-20
// Autor:  baltasarq@gmail.com


using System;
using System.IO;
using System.Text;

namespace Visor.Core {
	
	/// <summary>
	/// This is a buffer frame view over the file contents.
	/// </summary>
	public class Document {
		public enum ValueType { Txt, Hex, Dec };
		
		public const int Columns = 16;
		public const int MaxRows = 16;
		public const string Delimiters = ",;\t.:-/";
		
		private long position = 0;
		
		/// <summary>
		/// The position inside the File 
		/// </summary>
		public long Position {
			get { return this.position; }
			set {
				if ( value > -1
				  && value < this.FileLength )
				{
					long oldPos = this.position;
					this.position = value;
					this.CheckToInvalidateData( oldPos );
				}
			}
		}
		
		/// <summary>
		/// Determines whether the data block must be read from disk 
		/// </summary>
		public bool IsInvalidated
		{
			get; set;
		}
		
		/// <summary>
		/// The current frame buffer number 
		/// </summary>
		public long FrameBufferNumber {
			get { return ( this.Position / this.BufferSize ); }
			set {
				if ( value > -1 ) {
					this.Position = value * this.BufferSize;
				}
			}
		}
		
		/// <summary>
		/// Next read will happen on the next framebuffer 
		/// </summary>
		public void Advance()
		{
			this.Position += this.BufferSize;
		}
		
		/// <summary>
		/// Next read will happen in the previous framebuffer 
		/// </summary>
		public void Recede()
		{
			this.Position -= this.BufferSize;
		}
		
		private long totalFrameBuffersNumber = -1;
		
		/// <summary>
		/// Number of total frame buffers 
		/// </summary>
		public long TotalFrameBuffersNumber {
			get {
				if ( this.totalFrameBuffersNumber < 0 ) {
					this.totalFrameBuffersNumber = ( this.FileLength / this.BufferSize );
					
					if ( ( this.FileLength % this.BufferSize ) != 0 ) {
						++( this.totalFrameBuffersNumber );
					}
				}
				
				return this.totalFrameBuffersNumber;
			}
		}
		
		/// <summary>
		/// Invalidates data, provided the oldPos is different than current position 
		/// </summary>
		/// <param name="oldPos">
		/// A <see cref="System.Int32"/> The old position the document was in, as integer.
		/// </param>
		public bool CheckToInvalidateData(long oldPos)
		{
			this.IsInvalidated = false;
			
			if ( Position != oldPos ) {
				this.tabulatedData = null;
				this.IsInvalidated = true;
			}
			
			return this.IsInvalidated;
		}
		
		private byte[] data = null;
		private string[][] tabulatedData = null;
		
		/// <summary>
		/// Returns the raw data for this frame in the document
		/// </summary>
		public byte[] RawData {
			get { return this.data; }
		}
		
		/// <summary>
		/// The data inside the file (this buffer frame)
		/// The data is tabulated in tabulatedData.
		/// Each movement invalidates tabulatedData.
		/// </summary>
		public string[][] Data {
			get {
				if ( this.tabulatedData == null ) {
					if ( this.IsInvalidated ) {
						this.ReadCurrentFrameBuffer();
					}
					
					this.tabulatedData = new string[ this.data.Length / Columns ][];
					for(int i = 0; i < this.tabulatedData.Length; ++i) {
						this.tabulatedData[ i ] = new string[ Columns ];
					}
				
					for(int i = 0; i < this.data.Length; ++i) {
						this.tabulatedData[ i / Columns ][ i% Columns ] = this.data[ i ].ToString( "X2" );
					}
				}
				
				return this.tabulatedData;
			}
		}
		
		private FileStream file;
		
		/// <summary>
		/// The FileStream of the file to read 
		/// </summary>
		public FileStream File {
			get { return this.file; }
		}

		private string path;
		
		/// <summary>
		/// The path of the file 
		/// </summary>
		public string Path {
			get { return this.path; }
		}
		
		private int bufferSize = 256;
		
		/// <summary>
		/// The size of the frame buffer to use
		/// </summary>
		public int BufferSize {
			get { return this.bufferSize; }
			set {
				if ( value >= 512 ) {
					this.bufferSize = value;
					var oldData = this.RawData;
					this.data = new byte[ this.BufferSize ];
					oldData.CopyTo( this.data, 0 );
				}
			}
		}
		
		/// <summary>
		/// Returns the length of the file 
		/// </summary>
		public long FileLength {
			get { 	if ( file != null )
							return this.file.Length;
				  	else 	return 0;
			}
		}
		
		/// <summary>
		/// Create a document, attached to a given file 
		/// </summary>
		/// <param name="path">
		/// A <see cref="string"/> to the file to open
		/// </param>
		public Document(string path)
		{
			this.path = path;
			this.file = new FileStream( path.ToString(), FileMode.Open, FileAccess.Read );
			this.position = 0;
			this.data = new byte[ this.BufferSize ];
			this.IsInvalidated = true;
			this.ReadCurrentFrameBuffer();
		}
		
		~Document()
		{
			file.Close();
		}
		
		/// <summary>
		/// Reads the frame buffer marked by the current position 
		/// </summary>
		public void ReadCurrentFrameBuffer()
		{
			// Calculate position given buffer size
			for(int framePos = 0; framePos < this.FileLength; framePos += this.BufferSize) {
				if ( framePos >= this.Position ) {
					this.Position = framePos;
					break;
				}
			}
			
			// Determine number of bytes to read
			int count = this.BufferSize;
			if ( ( this.Position + this.BufferSize ) > this.FileLength ) {
				count = (int) ( this.FileLength - ( (long) this.Position ) );
			}
			
			// Prepare the data buffer
			if ( count < this.BufferSize ) {
				for(int i = Math.Max( 0, count -1 ); i < this.BufferSize; ++i) {
					this.data[ i ] = 0;
				}
			}
			
			// Read that buffer frame
			file.Position = this.Position;
			file.Read( this.data, 0, count );
		}
		
		/// <summary>
		/// Returns the position of the value to find, -1 if not found 
		/// </summary>
		/// <param name="txt">
		/// A <see cref="System.String"/> holding the value to look for
		/// </param>
		/// <param name="vt">
		/// A <see cref="ValueType"/> the type of the value to find
		/// </param>
		/// <returns>
		/// A <see cref="System.Int64"/> with the position of the of the value found, -1 if not found.
		/// </returns>
		public long Find(string txt, ValueType vt)
		{
			if ( vt == ValueType.Txt ) {
				return this.FindText( txt );
			}
			else {
				return this.FindByteSequence( DecodeTxtToByteArray( txt, vt ) );
			}
		}
		
		/// <summary>
		/// Looks for the text passed as parameter.
		/// </summary>
		/// <param name="txt">
		/// A <see cref="System.String"/> holding the text to find.
		/// </param>
		/// <returns>
		/// A <see cref="System.Int64"/> with the position of the of the value found, -1 if not found.
		/// </returns>
		public long FindText(string txt)
		{
			return this.FindByteSequence( Encoding.UTF8.GetBytes( txt ) );
		}
		
		/// <summary>
		/// Looks for the byte sequence passed as parameter.
		/// </summary>
		/// <param name="txt">
		/// A <see cref="System.Byte[]"/> holding the byte sequence to find.
		/// </param>
		/// <returns>
		/// A <see cref="System.Int64"/> with the position of the of the value found, -1 if not found.
		/// </returns>
		public long FindByteSequence(byte[] bs)
		{
			long i = -1;
			byte[] chunk;
			
			if ( bs != null ) {
				int chunkLength = bs.Length;
				
				// Look for the next instance of the byte block
				for(i = this.Position; i < this.FileLength; ++i) {
					byte b = (byte) this.File.ReadByte();
					
					if ( bs[ 0 ] == b ) {
						chunk = new byte[ bs.Length ];
						this.File.Seek( -1, SeekOrigin.Current );
						this.File.Read( chunk, 0, chunkLength );
						
						if ( isArrayEqualTo( bs, chunk ) ) {
							break;
						}
					}
				}
			}

			// Adapt i in case it was not found
			if ( i >= this.FileLength ) {
				i = -1;
			}
			
			return i;
		}
		
		public static bool isArrayEqualTo(byte[] array1, byte[] array2)
		{
			int i = 0;
			
			if ( array1.Length == array2.Length ) {
				for(; i < array2.Length; ++i) {
					if ( array1[ i ] != array2[ i ] ) {
						break;
					}
				}
			}
			
			return ( i >= array1.Length );
		}
		
		/// <summary>
		/// Returns the values coded in the text, as a vector.
		/// </summary>
		/// <param name="txt">
		/// A <see cref="System.String"/> with the string to look for.
		/// </param>
		/// <param name="vt">
		/// A <see cref="ValueType"/> holding the type of decimal values to decode.
		/// </param>
		/// <returns>
		/// A <see cref="System.Byte[]"/> sequence that holds the representation of the values to look for.
		/// </returns>
		public byte[] DecodeTxtToByteArray(string txt, ValueType vt)
		{
			byte[] toret = null;
			StringBuilder txtCopy = new StringBuilder();
			int fromBase = 10;
			
			// Decide base
			if ( vt == Document.ValueType.Hex ) {
				fromBase = 16;
			}
			
			// Changes all ',' and ';' to spaces
			for(int i = 0; i < txt.Length; ++i) {
				if ( Delimiters.IndexOf( txt[ i ] ) > -1 ) {
					txtCopy.Append( ' ' );
				} else {
					txtCopy.Append( txt[ i ] );
				}
			}
			
			// Divide string in its values
			string[] values = txtCopy.ToString().Split( new char[]{ ' ' }, StringSplitOptions.RemoveEmptyEntries );
			
			// Decode each value
			toret = new byte[ values.Length ];
			for(int i = 0; i < values.Length; ++i) {
				toret[ i ] = Convert.ToByte( values[ i ], fromBase );
			}
			
			return toret;
		}
		
		/// <summary>
		/// Returns whether a char is not printable 
		/// </summary>
		/// <param name="x">
		/// A <see cref="System.Char"/> containing the character
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/> holding true if the character is printable; false otherwise
		/// </returns>
		public static bool IsPrintable(char x)
		{
			return (
				   char.IsLetterOrDigit( x )
				|| char.IsPunctuation( x )
				|| char.IsSeparator( x )
				|| char.IsSymbol( x )
				|| char.IsWhiteSpace( x )
			);
		}
		
		/// <summary>
		/// Returns whether a char is strictly printable in one and only one position. 
		/// </summary>
		/// <param name="x">
		/// A <see cref="System.Char"/> holding the character.
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/> holdin true if is printable, false otherwise.
		/// </returns>
		public static bool IsStrictlyPrintable(char x)
		{
			if ( x == '\n'
			  || x == '\r'
			  || x == '\t' )
			{
				return false;
			}
			else return IsPrintable( x );
		}
		
	}
}
