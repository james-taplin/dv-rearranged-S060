using System;
using System.IO;
using System.Text;
using MPAPI.Interfaces.Packets;

namespace RearrangedS060.Multiplayer
{
    // The API transports packets reliably but unordered. State revisions live in
    // the validated core JSON; permission updates have their own revision.
    public abstract class BoundedPacket : ISerializablePacket
    {
        public bool Valid { get; private set; }
        public abstract void Serialize(BinaryWriter writer);
        protected abstract void Read(BinaryReader reader);

        public void Deserialize(BinaryReader reader)
        {
            Valid = false;
            try { Read(reader); Valid = true; }
            catch (InvalidDataException) { }
            catch (IOException) { }
            catch (ArgumentException) { }
            catch (OverflowException) { }
        }

        protected static string Text(BinaryReader reader, int maximumBytes)
        {
            // BinaryWriter string prefix: a 7-bit encoded UTF-8 byte count.
            // Check it before allocating, unlike BinaryReader.ReadString().
            uint length = 0;
            for (var shift = 0; shift < 35; shift += 7)
            {
                byte value = reader.ReadByte();
                if (shift == 28 && (value & 0xf8) != 0) throw new InvalidDataException("String length overflow.");
                length |= (uint)(value & 0x7f) << shift;
                if ((value & 0x80) != 0) continue;
                if (length > maximumBytes) throw new InvalidDataException("Packet string exceeds its limit.");
                byte[] bytes = reader.ReadBytes((int)length);
                if (bytes.Length != length) throw new EndOfStreamException();
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            throw new InvalidDataException("Invalid string length.");
        }
    }

    public sealed class HelloPacket : BoundedPacket
    {
        public int Protocol;
        public string Identity = "";
        public override void Serialize(BinaryWriter w) { w.Write(Protocol); w.Write(Identity); }
        protected override void Read(BinaryReader r) { Protocol = r.ReadInt32(); Identity = Text(r, 512); }
    }

    public sealed class WelcomePacket : BoundedPacket
    {
        public int Protocol;
        public string Identity = "";
        public bool Accepted, CanChange;
        public long PolicyRevision;
        public string Message = "";
        public override void Serialize(BinaryWriter w)
        {
            w.Write(Protocol); w.Write(Identity); w.Write(Accepted); w.Write(CanChange);
            w.Write(PolicyRevision); w.Write(Message);
        }
        protected override void Read(BinaryReader r)
        {
            Protocol = r.ReadInt32(); Identity = Text(r, 512); Accepted = r.ReadBoolean(); CanChange = r.ReadBoolean();
            PolicyRevision = r.ReadInt64(); Message = Text(r, 2048);
        }
    }

    public sealed class StatePacket : BoundedPacket
    {
        public string Json = "";
        public override void Serialize(BinaryWriter w) => w.Write(Json);
        protected override void Read(BinaryReader r) => Json = Text(r, 65536);
    }

    public sealed class RequestPacket : BoundedPacket
    {
        public string CarGuid = "";
        public int Arrangement;
        public long ExpectedRevision;
        public long Epoch;
        public override void Serialize(BinaryWriter w) { w.Write(CarGuid); w.Write(Arrangement); w.Write(ExpectedRevision); w.Write(Epoch); }
        protected override void Read(BinaryReader r) { CarGuid = Text(r, 128); Arrangement = r.ReadInt32(); ExpectedRevision = r.ReadInt64(); Epoch = r.ReadInt64(); }
    }

    public sealed class RequestResultPacket : BoundedPacket
    {
        public string Message = "";
        public override void Serialize(BinaryWriter w) => w.Write(Message);
        protected override void Read(BinaryReader r) => Message = Text(r, 2048);
    }

    public sealed class ApplyFailurePacket : BoundedPacket
    {
        public string Message = "";
        public override void Serialize(BinaryWriter w) => w.Write(Message);
        protected override void Read(BinaryReader r) => Message = Text(r, 2048);
    }
}
