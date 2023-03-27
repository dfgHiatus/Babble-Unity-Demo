using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Babble
{
    public class OscMessage
    {
        public readonly byte[] Data;
        public readonly string Address;
        public readonly object Value;

        private OscMessage(string name, char typeIdentifier)
        {
            Address = name;

            var nameBytes = Encoding.ASCII.GetBytes(name);
            nameBytes = nameBytes.EnsureCompliance();

            var valueIdentBytes = Encoding.ASCII.GetBytes("," + typeIdentifier);
            valueIdentBytes = valueIdentBytes.EnsureCompliance();

            Data = new byte[nameBytes.Length + valueIdentBytes.Length];
            Array.Copy(nameBytes, Data, nameBytes.Length);
            Array.Copy(valueIdentBytes, 0, Data, nameBytes.Length, valueIdentBytes.Length);
        }

        public OscMessage(string name, int value) : this(name, 'i')
        {
            var valueArr = BitConverter.GetBytes(value);
            Array.Reverse(valueArr);

            var newFullArr = new byte[Data.Length + valueArr.Length];
            Array.Copy(Data, newFullArr, Data.Length);
            Array.Copy(valueArr, 0, newFullArr, Data.Length, valueArr.Length);
            Data = newFullArr;
        }

        public OscMessage(string name, double value) : this(name, 'f')
        {
            var valueArr = BitConverter.GetBytes((float)value);
            Array.Reverse(valueArr);

            var newFullArr = new byte[Data.Length + valueArr.Length];
            Array.Copy(Data, newFullArr, Data.Length);
            Array.Copy(valueArr, 0, newFullArr, Data.Length, valueArr.Length);
            Data = newFullArr;
        }

        public OscMessage(string name, bool value) : this(name, value ? 'T' : 'F') { }

        public OscMessage(string name, char type, byte[] valueBytes) : this(name, type)
        {
            if (valueBytes == null) return;
            var newFullArr = new byte[Data.Length + valueBytes.Length];
            Array.Copy(Data, newFullArr, Data.Length);
            Array.Copy(valueBytes, 0, newFullArr, Data.Length, valueBytes.Length);
            Data = newFullArr;
        }

        public OscMessage(byte[] bytes)
        {
            int iter = 0;

            var addressBytes = new List<byte>();
            for (; iter < bytes.Length; iter++)
            {
                if (bytes[iter] == 0)
                    break;

                addressBytes.Add(bytes[iter]);
            }

            Address = Encoding.ASCII.GetString(addressBytes.ToArray());

            // In the event we pass in an invalid byte array, fly
            if (Address.IndexOf(',') == -1) return;

            // Increase iter until we find the type identifier
            for (; iter < bytes.Length; iter++)
            {
                if (bytes[iter] == ',')
                {
                    iter++;
                    break;
                }
            }

            byte type = bytes[iter];
            iter += 2; // Next multiple of 4

            switch (type)
            {
                #region Standard OSC-Type/s

                case 105: // OSC Type tag: 'i' ; int32
                    var intBytes = new byte[4];
                    Array.Copy(bytes, iter, intBytes, 0, 4);
                    Array.Reverse(intBytes);
                    Value = BitConverter.ToInt32(intBytes, 0);
                    break;
                case 102: // OSC Type tag: 'f' ; float32
                    var floatBytes = new byte[4];
                    Array.Copy(bytes, iter, floatBytes, 0, 4);
                    Array.Reverse(floatBytes);
                    Value = BitConverter.ToSingle(floatBytes, 0);
                    break;
                case 115: // OSC Type tag: 's' ; OSC-string
                    var stringBytes = new List<byte>();
                    for (iter++; iter < bytes.Length; iter++)
                    {
                        if (bytes[iter] == 0)
                            break;

                        stringBytes.Add(bytes[iter]);
                    }
                    Value = Encoding.ASCII.GetString(stringBytes.ToArray());
                    break;
                case 98: // OSC Type tag: 'b' ; OSC-blob
                    goto default;

                #endregion

                #region Non Standard OSC-Type/s

                case 104: // OSC Type tag: 'h' ; 64 Bit Big-Endian
                    goto default;
                case 116: // OSC Type tag: 't' ; OSC-timetag
                    goto default;
                case 100: // OSC Type tag: 'd' ; 64 Bit Big-Endian
                    goto default;
                case 83: // OSC Type tag: 'S' ; Type represented in OSC-string
                    goto default;
                case 99: // OSC Type tag: 'c' ; 32 bit ASCII
                    goto default;
                case 114: // OSC Type tag: 'r' ; 32 bit RGBA color
                    goto default;
                case 109: // OSC Type tag: 'm' ; 4 bit MIDI. Each byte as: port id, status, data1, data2
                    goto default;
                case 70: // OSC Type tag: 'T' ; Represents true, No extra data
                    Value = false;
                    break;
                case 84: // OSC Type tag: 'F' ; Represents false, No extra data
                    Value = true;
                    break;
                case 78: // OSC Type tag: 'N' ; Represents NIL (zero), No extra data
                    Value = 0;
                    break;
                case 73: // OSC Type tag: 'I' ; Represents Infinitum (endlessly infinite), No extra data. Capping to 1.
                    Value = 1.0f;
                    break;
                case 91: // OSC Type tag: '[' ; Represents start of an array.
                    goto default;
                case 93: // OSC Type tag: ']' ; Represents end of an array.
                    goto default;

                #endregion

                default:
                    Debug.LogError("OSC Type unimplemented: " + type + " for name " + Address);
                    break;
            }
        }
    }
}