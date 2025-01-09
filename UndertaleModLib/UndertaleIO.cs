using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModLib
{
    /**
     * TODO: This is not the cleanest implementation, but I was focusing on clean interface.
     * Could probably use some refactoring or a complete rewrite.
     */

    public interface UndertaleResourceRef : UndertaleObject
    {
        object Resource { get; set; }
        void PostUnserialize(UndertaleReader reader);
        int SerializeById(UndertaleWriter writer);
    }

    public class UndertaleResourceById<T, ChunkT> : UndertaleResourceRef, IStaticChildObjectsSize, IDisposable where T : UndertaleResource, new() where ChunkT : UndertaleListChunk<T>
    {
        /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
        public static readonly uint ChildObjectsSize = 4;

        public int CachedId { get; set; } = -1;
        public T Resource { get; set; }

        object UndertaleResourceRef.Resource { get => Resource; set => Resource = (T)value; }

        public UndertaleResourceById()
        {
            this.CachedId = -1;
        }

        public UndertaleResourceById(int id = -1)
        {
            this.CachedId = id;
        }

        public UndertaleResourceById(T res)
        {
            this.Resource = res;
        }

        public UndertaleResourceById(T res, int id = -1)
        {
            this.Resource = res;
            this.CachedId = id;
        }

        private static ChunkT FindListChunk(UndertaleData data)
        {
            if (data.FORM.ChunksTypeDict.TryGetValue(typeof(ChunkT), out UndertaleChunk chunk))
            {
                return (ChunkT)chunk;
            }
            return null;
        }

        public int SerializeById(UndertaleWriter writer)
        {
            ChunkT chunk = FindListChunk(writer.undertaleData);
            if (chunk != null)
            {
                if (Resource != null)
                {
                    CachedId = chunk.IndexDict[Resource];
                    if (CachedId < 0)
                        throw new IOException("Unregistered object");
                }
                else
                {
                    if (typeof(ChunkT) == typeof(UndertaleChunkAGRP))
                        CachedId = 0;
                    else
                        CachedId = -1;
                }
            }
            return CachedId;
        }

        public void UnserializeById(UndertaleReader reader, int id)
        {
            if (id < -1)
                throw new IOException("Invalid value for resource ID (" + typeof(ChunkT).Name + "): " + id);
            CachedId = id;
            reader.RequestResourceUpdate(this);
        }

        public void PostUnserialize(UndertaleReader reader)
        {
            IList<T> list = FindListChunk(reader.undertaleData)?.List;
            if (list != null)
            {
                if (typeof(ChunkT) == typeof(UndertaleChunkAGRP) && CachedId == reader.undertaleData.GetBuiltinSoundGroupID() && list.Count == 0) // I won't even ask why this works like that
                {
                    Resource = default;
                    return;
                }
                if (CachedId >= list.Count)
                {
                    reader.SubmitWarning("Invalid value for resource ID of type " + typeof(ChunkT).Name + ": " + CachedId + " (there are only " + list.Count + ")");
                    return;
                }
                Resource = CachedId >= 0 ? list[CachedId] : default;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return (Resource?.ToString() ?? "(null)") + GetMarkerSuffix();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Resource = default;
        }

        public string GetMarkerSuffix()
        {
            return "@" + CachedId;
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.Write(SerializeById(writer));
        }

        public void Unserialize(UndertaleReader reader)
        {
            UnserializeById(reader, reader.ReadInt32());
        }
    }

    public class UndertaleReader : AdaptiveBinaryReader
    {
        /// <summary>
        /// function to delegate warning messages to
        /// </summary>
        /// <param name="warning"></param>
        public delegate void WarningHandlerDelegate(string warning);
        /// <summary>
        /// function to delegate informational messages to
        /// </summary>
        /// <param name="message"></param>
        public delegate void MessageHandlerDelegate(string message);
        private WarningHandlerDelegate WarningHandler;
        private MessageHandlerDelegate MessageHandler;

        public bool ReadOnlyGEN8 { get; set; }

        /// <summary>
        /// The detected absolute path of the data file, if a FileStream is passed in, or null otherwise (by default).
        /// Can also be manually changed.
        /// </summary>
        public string FilePath { get; set; } = null;

        /// <summary>
        /// The detected absolute path of the directory containing the data file, if a FileStream is passed in, or null otherwise (by default).
        /// Can also be manually changed.
        /// </summary>
        public string Directory { get; set; } = null;

        internal readonly record struct BytecodeInformation(uint InstructionCount, UndertaleCode RootEntry);

        internal Dictionary<uint, BytecodeInformation> BytecodeAddresses;
        internal ArrayPool<uint> ListPtrsPool = ArrayPool<uint>.Create(100000, 17);
        internal string LastChunkName;
        internal List<string> AllChunkNames;
        internal bool Bytecode14OrLower = false;

        public UndertaleReader(Stream input,
                               WarningHandlerDelegate warningHandler = null, MessageHandlerDelegate messageHandler = null,
                               bool onlyGeneralInfo = false) : base(input)
        {
            WarningHandler = warningHandler;
            MessageHandler = messageHandler;
            ReadOnlyGEN8 = onlyGeneralInfo;
            if (input is FileStream fs)
            {
                FilePath = fs.Name;
                Directory = Path.GetDirectoryName(FilePath);
            }

            FillUnserializeCountDictionaries();
        }

        // TODO: This would be more useful if it reported location like the exceptions did
        public void SubmitWarning(string warning)
        {
            if (WarningHandler != null)
                WarningHandler.Invoke(warning);
            else
                throw new IOException(warning);
        }

        public void SubmitMessage(string message)
        {
            if (MessageHandler != null)
                MessageHandler.Invoke(message);
            else
                Debug.WriteLine(message);
        }

        public UndertaleChunk ReadUndertaleChunk()
        {
            return UndertaleChunk.Unserialize(this);
        }
        public uint CountChunkChildObjects()
        {
            return UndertaleChunk.CountChunkChildObjects(this);
        }

        private List<UndertaleResourceRef> resUpdate = new List<UndertaleResourceRef>();
        internal UndertaleData undertaleData;

        public UndertaleData ReadUndertaleData()
        {
            UndertaleData data = new UndertaleData();
            undertaleData = data;

            resUpdate.Clear();

            string name = ReadChars(4);
            if (name != "FORM")
                throw new IOException("Root chunk is " + name + " not FORM");
            uint length = ReadUInt32();
            data.FORM = new UndertaleChunkFORM();
            DebugUtil.Assert(data.FORM.Name == name);
            data.FORM.Length = length;

            long startPos = Position;
            uint poolSize = 0;
            if (!ProcessCountExc()) // process an exception from "FillUnserializeCountDictionaries()"
            {
                try
                {
                    if (!ReadOnlyGEN8)
                        poolSize = data.FORM.UnserializeObjectCount(this);
                }
                catch (Exception e)
                {
                    countUnserializeExc = e;
                    Debug.WriteLine(e);

                    SwitchReaderType(false);
                }
            }
            ListPtrsPool = null;

            InitializePools(poolSize);

            Position = startPos;

            var lenReader = EnsureLengthFromHere(data.FORM.Length);
            data.FORM.UnserializeChunk(this);
            lenReader.ToHere();

            SubmitMessage("Resolving resource IDs...");
            foreach (UndertaleResourceRef res in resUpdate)
                res.PostUnserialize(this);
            resUpdate.Clear();

            // Skip if it's "audiogroup*.dat" file
            if (!FilePath.EndsWith(".dat"))
            {
                data.BuiltinList = new BuiltinList(data);
                Decompiler.GameSpecificResolver.Initialize(data);
                UndertaleEmbeddedTexture.FindAllTextureInfo(data);
            }

            // Iterate over function names to see if 2023.11+ naming process was used (if necessary)
            if (data.Functions is not null && data.IsVersionAtLeast(2023, 8) && !data.IsVersionAtLeast(2023, 11))
            {
                foreach (UndertaleFunction function in data.Functions)
                {
                    // If name starts with "gml_Script" and contains a @ character, it should be from 2023.11
                    if (function.Name.Content is string functionName &&
                        functionName.StartsWith("gml_Script_", StringComparison.Ordinal) && 
                        functionName.Contains('@'))
                    {
                        data.SetGMS2Version(2023, 11);
                        break;
                    }
                }
            }

            ProcessCountExc(poolSize);

            return data;
        }

        internal void RequestResourceUpdate(UndertaleResourceRef res)
        {
            resUpdate.Add(res);
        }

        public override bool ReadBoolean()
        {
            uint a = ReadUInt32();
            if (a == 0)
                return false;
            if (a == 1)
                return true;
            throw new IOException("Invalid boolean value: " + a);
        }

        private Dictionary<uint, UndertaleObject> objectPool;
        private Dictionary<UndertaleObject, uint> objectPoolRev;
        private HashSet<uint> unreadObjects = new HashSet<uint>();

        private Exception countUnserializeExc = null;
        private readonly Dictionary<Type, Func<UndertaleReader, uint>> unserializeFuncDict = new();
        private readonly Dictionary<Type, uint> staticObjCountDict = new();
        private readonly Dictionary<Type, uint> staticObjSizeDict = new();

        private readonly BindingFlags publicStaticFlags
            = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
        private readonly Type[] readerArgType = { typeof(UndertaleReader) };
        private readonly Type delegateType = typeof(Func<UndertaleReader, uint>);
        private readonly Func<UndertaleReader, uint> blankCountFunc = new(_ => { return 0; });

        private bool ProcessCountExc(uint poolSize = 0)
        {
            if (countUnserializeExc is not null)
            {
                try
                {
                    string fileDir = Path.GetDirectoryName(Environment.ProcessPath);
                    File.WriteAllText(Path.Combine(fileDir, "unserializeCountError.txt"),
                                      countUnserializeExc.ToString() + "\n"
                                      + countUnserializeExc.Message + "\n"
                                      + countUnserializeExc.StackTrace);

                    SubmitWarning("Warning - there was an error while trying to unserialize total object count.\n" +
                                  "The error log is saved to \"unserializeCountError.txt\"." +
                                  "Please report that error to UndertaleModTool GitHub.");
                }
                catch { }

                countUnserializeExc = null;

                return true;
            }

            if (poolSize != 0 && poolSize != objectPool.Count)
            {
                SubmitWarning("Warning - the estimated object pool size differs from the actual size.\n" +
                             $"Estimated: {poolSize}, Actual: {objectPool.Count}\n" +
                              "Please report this on UndertaleModTool GitHub.");
            }

            return false;
        }
        private void FillUnserializeCountDictionaries()
        {
            try
            {
                Assembly currAssem = Assembly.GetExecutingAssembly();
                Type[] allTypes = currAssem.GetTypes();

                Type utObjectType = typeof(UndertaleObject);
                Type staticObjCountType = typeof(IStaticChildObjCount);
                Type staticObjSizeType = typeof(IStaticChildObjectsSize);

                allTypes = allTypes.Where(t => t.IsAssignableTo(utObjectType)).ToArray();
                foreach (Type t in allTypes)
                {
                    // It's not possible to call a static method of generic classes without present type argument.
                    if (t.ContainsGenericParameters)
                        continue;

                    MethodInfo mi = t.GetMethod("UnserializeChildObjectCount", publicStaticFlags, readerArgType);
                    if (mi is null)
                        continue;

                    var func = Delegate.CreateDelegate(delegateType, mi) as Func<UndertaleReader, uint>;
                    if (func is null)
                    {
                        Debug.WriteLine($"Can't create a delegate from MethodInfo of type \"{t.FullName}\"");
                        continue;
                    }

                    unserializeFuncDict[t] = func;
                }

                for (int i = 0; i < allTypes.Length; i++)
                {
                    Type t = allTypes[i];
                    FieldInfo fi;
                    object res;

                    // It's not supported to get a static field from generic classes without present type argument.
                    if (t.ContainsGenericParameters)
                        continue;

                    if (t.IsAssignableTo(staticObjCountType))
                    {
                        fi = t.GetField("ChildObjectCount", publicStaticFlags);
                        if (fi is null)
                        {
                            Debug.WriteLine($"Can't get \"ChildObjectCount\" field of \"{t.FullName}\"");
                            continue;
                        }

                        res = fi.GetValue(null);
                        if (res is null)
                        {
                            Debug.WriteLine($"Can't get value of \"ChildObjectCount\" of \"{t.FullName}\"");
                            continue;
                        }

                        staticObjCountDict[t] = (uint)res;
                    }

                    if (t.IsAssignableTo(staticObjSizeType))
                    {
                        fi = t.GetField("ChildObjectsSize", publicStaticFlags);
                        if (fi is null)
                        {
                            Debug.WriteLine($"Can't get \"ChildObjectsSize\" field of \"{t.FullName}\"");
                            continue;
                        }

                        res = fi.GetValue(null);
                        if (res is null)
                        {
                            Debug.WriteLine($"Can't get value of \"ChildObjectsSize\" of \"{t.FullName}\"");
                            continue;
                        }

                        staticObjSizeDict[t] = (uint)res;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                countUnserializeExc = e;
            }
        }
        public Func<UndertaleReader, uint> GetUnserializeCountFunc(Type objType)
        {
            if (!unserializeFuncDict.TryGetValue(objType, out var res))
            {
                MethodInfo mi = objType.GetMethod("UnserializeChildObjectCount", publicStaticFlags, readerArgType);
                if (mi is null)
                {
                    Debug.WriteLine($"\"UndertaleReader.unserializeFuncDict\" doesn't contain a method for \"{objType.FullName}\".");
                    return blankCountFunc;
                }

                //Debug.WriteLine($"Adding a generic class method for \"{objType.FullName}\" to \"UndertaleReader.unserializeFuncDict\".");

                var func = Delegate.CreateDelegate(delegateType, mi) as Func<UndertaleReader, uint>;
                if (func is null)
                {
                    Debug.WriteLine($"Can't create a delegate from MethodInfo of type \"{objType.FullName}\"");
                    return blankCountFunc;
                }

                unserializeFuncDict[objType] = func;

                res = func;
            }

            return res;
        }
        public uint GetStaticChildCount(Type objType)
        {
            if (!staticObjCountDict.TryGetValue(objType, out uint res))
            {
                Debug.WriteLine($"\"UndertaleReader.staticObjCountDict\" doesn't contain type \"{objType.FullName}\".");
                return 0;
            }

            return res;
        }
        public uint GetStaticChildObjectsSize(Type objType)
        {
            if (!staticObjSizeDict.TryGetValue(objType, out uint res))
            {
                Debug.WriteLine($"\"UndertaleReader.staticObjSizeDict\" doesn't contain type \"{objType.FullName}\".");
                return 0;
            }

            return res;
        }
        public void SetStaticChildCount(Type objType, uint count)
        {
            staticObjCountDict[objType] = count;
        }
        public void SetStaticChildObjectsSize(Type objType, uint size)
        {
            staticObjSizeDict[objType] = size;
        }

        public Dictionary<uint, UndertaleObject> GetOffsetMap()
        {
            return objectPool;
        }

        public Dictionary<UndertaleObject, uint> GetOffsetMapRev()
        {
            return objectPoolRev;
        }

        public void InitializePools(uint objCount = 0)
        {
            if (objCount == 0)
            {
                objectPool = new();
                objectPoolRev = new();
            }
            else
            {
                int objCountInt = (int)objCount;
                objectPool = new(objCountInt);
                objectPoolRev = new(objCountInt);
            }
        }

        public uint GetChildObjectCount(Type t)
        {
            if (!unserializeFuncDict.TryGetValue(t, out var func))
            {
                if (staticObjSizeDict.TryGetValue(t, out uint size))
                {
                    Position += size;

                    staticObjCountDict.TryGetValue(t, out uint subCount);

                    return subCount;
                }

                throw new UndertaleSerializationException(
                    $"\"UndertaleReader.unserializeFuncDict\" doesn't contain a method for \"{t.FullName}\".");
            }

            return func(this);
        }
        public uint GetChildObjectCount<T>() where T : UndertaleObject
        {
            Type t = typeof(T);

            return GetChildObjectCount(t);
        }
        

        public T GetUndertaleObjectAtAddress<T>(uint address) where T : UndertaleObject, new()
        {
            if (address == 0)
                return default;
            UndertaleObject obj;
            if (!objectPool.TryGetValue(address, out obj))
            {
                obj = new T();
                objectPool.Add(address, obj);
                objectPoolRev.Add(obj, address);
                unreadObjects.Add(address);
            }
            return (T)obj;
        }

        public uint GetAddressForUndertaleObject(UndertaleObject obj)
        {
            if (obj == null)
                return 0;
            return objectPoolRev[obj];
        }

        public void ReadUndertaleObject<T>(T obj) where T : UndertaleObject, new()
        {
            try
            {
                var expectedAddress = GetAddressForUndertaleObject(obj);
                if (expectedAddress == 0)
                    return;
                if (expectedAddress != AbsPosition)
                {
                    SubmitWarning("Reading misaligned at " + AbsPosition.ToString("X8") + ", realigning back to " + expectedAddress.ToString("X8") + "\nHIGH RISK OF DATA LOSS! The file is probably corrupted, or uses unsupported features\nProceed at your own risk");
                    AbsPosition = expectedAddress;
                }
                unreadObjects.Remove((uint)AbsPosition);
                obj.Unserialize(this);
            }
            catch (Exception e)
            {
                throw new UndertaleSerializationException(e.Message + "\nat " + AbsPosition.ToString("X8") + " while reading object " + typeof(T).FullName, e);
            }
        }

        public T ReadUndertaleObject<T>() where T : UndertaleObject, new()
        {
            uint address = (uint)AbsPosition;

            T result;
            if (objectPool.TryGetValue(address, out UndertaleObject obj))
            {
                result = (T)obj;
                unreadObjects.Remove(address);
            }
            else
            {
                result = new T();
                objectPool.Add(address, result);
                objectPoolRev.Add(result, address);
            }

            result.Unserialize(this);
            return result;
        }

        public T ReadUndertaleObjectPointer<T>() where T : UndertaleObject, new()
        {
            return GetUndertaleObjectAtAddress<T>(ReadUInt32());
        }

        public UndertaleString ReadUndertaleString()
        {
            uint addr = ReadUInt32();

            if (addr == 0)
                return null;

            // Normally, the strings point directly to the string content
            // This may be done that way because it's faster when the game accesses them, but for our purposes it's better to access the whole string resource object
            return GetUndertaleObjectAtAddress<UndertaleString>(addr - 4);
        }

        public void ThrowIfUnreadObjects()
        {
            if (ReadOnlyGEN8)
                return;

            if (unreadObjects.Count > 0)
            {
                throw new IOException("Found pointer targets that were never read:\n" + String.Join("\n", unreadObjects.Take(10).Select((x) => "0x" + x.ToString("X8") + " (" + objectPool[x].GetType().Name + ")")) + (unreadObjects.Count > 10 ? "\n(and more, " + unreadObjects.Count + " total)" : ""));
            }
        }

        public class EnsureLengthOperation
        {
            private readonly UndertaleReader reader;
            private readonly int startPos;
            private readonly uint expectedLength;
            internal EnsureLengthOperation(UndertaleReader reader, uint expectedLength)
            {
                this.reader = reader;
                this.startPos = (int)reader.Position;
                this.expectedLength = expectedLength;
            }
            public void ToHere()
            {
                int endPos = (int)reader.Position;
                uint length = (uint)(endPos - startPos);
                if (length != expectedLength)
                {
                    int diff = (int)expectedLength - (int)length;
                    reader.SubmitWarning("WARNING: File specified length " + expectedLength + ", but read only " + length + " (" + diff + " padding?)");
                    if (diff > 0)
                        reader.Position += (uint)diff;
                    else
                        throw new IOException("Read underflow");
                }
            }
        }

        public void Align(int alignment, byte paddingbyte = 0x00)
        {
            while ((AbsPosition & (alignment - 1)) != paddingbyte)
            {
                DebugUtil.Assert(ReadByte() == paddingbyte, "Invalid alignment padding");
            }
        }

        public EnsureLengthOperation EnsureLengthFromHere(uint expectedLength)
        {
            return new EnsureLengthOperation(this, expectedLength);
        }
    }

    public class UndertaleWriter : FileBinaryWriter
    {
        internal UndertaleData undertaleData;

        public string LastChunkName;
        public uint LastBytecodeAddress = 0;
        public bool Bytecode14OrLower;

        public delegate void MessageHandlerDelegate(string message);
        private MessageHandlerDelegate MessageHandler;

        public UndertaleWriter(Stream output, MessageHandlerDelegate messageHandler = null) : base(output)
        {
            MessageHandler = messageHandler;
        }

        public void SubmitMessage(string message)
        {
            if (MessageHandler != null)
                MessageHandler.Invoke(message);
            else
                Debug.WriteLine(message);
        }

        public void Write(UndertaleChunk obj)
        {
            obj.Serialize(this);
        }

        public override void Write(bool b)
        {
            Write(b ? (uint)1 : (uint)0);
        }

        public void WriteUndertaleData(UndertaleData data)
        {
            undertaleData = data;
            Bytecode14OrLower = data?.GeneralInfo?.BytecodeVersion <= 14;

            // Figure out the last chunk by iterating identically as it does when serializing,
            // and generate the object index dictionaries for acceleration of "UndertaleResourceById.SerializeById()"
            foreach (var chunk in data.FORM.Chunks)
            {
                LastChunkName = chunk.Key;

                if (chunk.Value is IUndertaleListChunk listChunk)
                {
                    listChunk.GenerateIndexDict();
                }
            }

            Write(data.FORM);
        }

        private Dictionary<UndertaleObject, uint> objectPool = new();
        private Dictionary<UndertaleObject, List<uint>> pendingWrites = new();
        private Dictionary<UndertaleObject, List<uint>> pendingStringWrites = new();

        public void Flush(UndertaleData data)
        {
            // Clear out index dictionaries (no longer needed)
            foreach (var chunk in data.FORM.Chunks.Values)
            {
                if (chunk is IUndertaleListChunk listChunk)
                {
                    listChunk.ClearIndexDict();
                }
            }

            SubmitMessage("Flushing remaining buffer data...");
            base.Flush();
        }

        public Dictionary<UndertaleObject, uint> GetObjectPool()
        {
            return objectPool;
        }

        public uint GetAddressForUndertaleObject(UndertaleObject obj)
        {
            if (obj == null)
                return 0;
            if (!objectPool.TryGetValue(obj, out uint res))
                throw new KeyNotFoundException();
            return res;
        }

        public void WriteUndertaleObject<T>(T obj) where T : UndertaleObject, new()
        {
            try
            {
                // Store object address before writing it
                uint objectAddr = Position;

                if (typeof(T) == typeof(UndertaleString))
                {
                    // Can skip adding strings to object pool (nothing later in the file references them).
                    obj.Serialize(this);

                    // Patch all pointers to this string, if applicable
                    if (pendingStringWrites.TryGetValue(obj, out List<uint> patches))
                    {
                        uint returnTo = Position;
                        objectAddr += 4; // Destination is where the string starts, not its length
                        foreach (uint pointerAddr in patches)
                        {
                            Position = pointerAddr;
                            Write(objectAddr);
                        }
                        Position = returnTo;

                        // Remove pending write
                        pendingStringWrites.Remove(obj);
                    }
                }
                else
                {
                    // Add object to pool
                    objectPool.Add(obj, objectAddr);

                    // Serialize object
                    obj.Serialize(this);

                    // Patch all pointers to this object, if applicable
                    if (pendingWrites.TryGetValue(obj, out List<uint> patches))
                    {
                        uint returnTo = Position;
                        foreach (uint pointerAddr in patches)
                        {
                            Position = pointerAddr;
                            Write(objectAddr);
                        }
                        Position = returnTo;

                        // Remove pending write
                        pendingWrites.Remove(obj);
                    }
                }
            }
            catch (Exception e)
            {
                throw new UndertaleSerializationException(e.Message + "\nat " + Position.ToString("X8") + " while writing object " + typeof(T).FullName, e);
            }
        }

        public void WriteUndertaleObjectPointer<T>(T obj) where T : UndertaleObject, new()
        {
            if (obj == null)
            {
                Write(0x00000000u);
                return;
            }

            if (objectPool.ContainsKey(obj))
            {
                Write(objectPool[obj]);
            }
            else
            {
                if (!pendingWrites.TryGetValue(obj, out List<uint> list))
                {
                    pendingWrites.Add(obj, list = new List<uint>());
                }
                list.Add(Position);
                Write(0xDEADC0DEu);
            }
        }

        public void WriteUndertaleString(UndertaleString obj)
        {
            if (obj == null)
            {
                Write(0x0000000u);
                return;
            }

            if (!pendingStringWrites.TryGetValue(obj, out List<uint> list))
            {
                pendingStringWrites.Add(obj, list = new List<uint>());
            }
            list.Add(Position);
            Write(0xDEADC0DEu);
        }

        public void ThrowIfUnwrittenObjects()
        {
            if ((pendingWrites.Count + pendingStringWrites.Count) != 0)
            {
                var unwrittenObjects = pendingWrites.Concat(pendingStringWrites);
                throw new IOException("Found pointer targets that were never written:\n"
                                      + String.Join("\n", unwrittenObjects.Take(10).Select((x) => x.Key + " at " + String.Join(", ", x.Value.Select((y) => "0x" + y.ToString("X8")))))
                                      + (unwrittenObjects.Count() > 10
                                         ? "\n(and more, " + unwrittenObjects.Count() + " total)"
                                         : ""));
            }
        }

        public void Align(int alignment, byte paddingbyte = 0x00)
        {
            while ((Position & (alignment - 1)) != paddingbyte)
            {
                Write(paddingbyte);
            }
        }

        public class WriteLengthOperation
        {
            private readonly UndertaleWriter writer;
            private readonly uint writePos;
            private uint? startPos = null;
            internal WriteLengthOperation(UndertaleWriter writer)
            {
                this.writer = writer;
                this.writePos = writer.Position;
                writer.Write(0xDEADC0DEu);
            }
            public void FromHere()
            {
                this.startPos = writer.Position;
            }
            public uint ToHere()
            {
                if (!startPos.HasValue)
                    throw new InvalidOperationException("Forgot to call FromHere()");

                uint endPos = writer.Position;
                writer.Position = writePos;
                uint valueToWrite = endPos - startPos.Value;
                writer.Write(valueToWrite);
                writer.Position = endPos;
                return valueToWrite;
            }
        }

        public WriteLengthOperation WriteLengthHere()
        {
            return new WriteLengthOperation(this);
        }
    }

    public static class UndertaleIO
    {
        public static UndertaleData Read(Stream stream, UndertaleReader.WarningHandlerDelegate warningHandler = null,
                                                        UndertaleReader.MessageHandlerDelegate messageHandler = null,
                                                        bool onlyGeneralInfo = false)
        {
            UndertaleReader reader = new(stream, warningHandler, messageHandler, onlyGeneralInfo);
            var data = reader.ReadUndertaleData();
            reader.ThrowIfUnreadObjects();
            return data;
        }

        public static void Write(Stream stream, UndertaleData data, UndertaleWriter.MessageHandlerDelegate messageHandler = null)
        {
            UndertaleWriter writer = new(stream, messageHandler);
            writer.WriteUndertaleData(data);
            writer.ThrowIfUnwrittenObjects();
            writer.Flush(data);
        }

        public static Dictionary<uint, UndertaleObject> GenerateOffsetMap(Stream stream)
        {
            UndertaleReader reader = new(stream);
            reader.ReadUndertaleData();
            reader.ThrowIfUnreadObjects();
            return reader.GetOffsetMap();
        }
    }
}
