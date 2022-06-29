using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class Converter
    {
        public static byte[] ToBytes(Message msj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(msj.ClientId);
                bw.Write(msj.Name);
                bw.Write(msj.Text);
                bw.Write(msj.Status);

                return ms.ToArray();
            };
        }
        public static Message FromBytes(byte[] buffer)
        {
            Message retVal = new Message();

            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryReader br = new BinaryReader(ms);
                retVal.ClientId = br.ReadInt32();
                retVal.Name = br.ReadString();
                retVal.Text = br.ReadString();
                retVal.Status = br.ReadString();
            }

            return retVal;
        }
    }
}
