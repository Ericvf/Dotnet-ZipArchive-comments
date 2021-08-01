using System;
using System.IO;
using System.Text;

namespace RawCommentsFromZip
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileName = args[0];
            var comments = GetZipFileComments(fileName);

            Console.WriteLine(comments);
            Console.Read();
        }

        /// <summary>
        /// Tries to read the comment of a ZipFile. For more information see:
        /// https://support.pkware.com/display/PKZIP/APPNOTE
        /// https://pkware.cachefly.net/webdocs/APPNOTE/APPNOTE-1.0.txt
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetZipFileComments(string fileName)
        {
            const uint end_of_central_dir_signature = 0x06054b50;
            const uint local_file_header_signature = 0x04034b50;

            // Open the filestream
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                // Create a binary reader 
                using (var binaryReader = new BinaryReader(fileStream))
                {
                    // Read the first 4 bytes and check for a local file header. This header determines if we continue parsing the file.
                    if (binaryReader.ReadUInt32() != local_file_header_signature)
                        return string.Empty;

                    // Move the stream pointer to the 22 bytes before the end of the stream. 
                    // The minimum size of a end of central directory header is 22 bytes.
                    fileStream.Seek(-22, SeekOrigin.End);

                    do
                    {
                        // Read 4 bytes and check if we have stumbled upon the zipfile header that contains the comments
                        if (binaryReader.ReadUInt32() == end_of_central_dir_signature)
                        {
                            // At this point 4 of the 22 bytes have already been read
                            // Advance the position of the stream pointer with 16 bytes
                            fileStream.Position += 16;

                            // Read 2 bytes for zipfile comment length, all 22 bytes will be read at this point
                            var zipFileCommentLength = binaryReader.ReadUInt16();
                            if (zipFileCommentLength > 0)
                            {
                                // Read the variable size zipfile comment
                                var zipFileCommentBuffer = binaryReader.ReadBytes(zipFileCommentLength);

                                // Parse comment buffer to string
                                return Encoding.UTF7.GetString(zipFileCommentBuffer);
                            }
                            else break;
                        }
                        else
                        {
                            fileStream.Seek(-5, SeekOrigin.Current);
                        }
                    }
                    while (fileStream.Position >= 5);
                }
            }

            return string.Empty;
        }
    }
}
