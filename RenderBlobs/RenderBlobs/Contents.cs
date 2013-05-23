using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RenderBlobs
{
    abstract class ContentNode
    {
        public string Name { get; set; }

        public virtual void PrettyPrint(TextWriter writer, int depth)
        {
            writer.WriteLine("{0}{1}", Indent(depth), Name);
        }

        public abstract JToken ToJson();

        protected static string Indent(int depth)
        {
            string s = string.Empty;
            while (depth-- > 0)
            {
                s += "  ";
            }
            return s;
        }
    }

    class ContentFolder : ContentNode
    {
        public IList<ContentNode> Children { get; private set; }

        public ContentFolder()
        {
            Children = new List<ContentNode>();
        }

        public override void PrettyPrint(TextWriter writer, int depth)
        {
            writer.WriteLine("{0}{1}", Indent(depth), Name);
            foreach (ContentNode child in Children)
            {
                child.PrettyPrint(writer, depth + 1);
            }
        }

        public override JToken ToJson()
        {
            JArray array = new JArray();

            foreach (ContentNode child in Children)
            {
                array.Add(child.ToJson());
            }

            JObject obj = new JObject();
            obj[Name] = array;

            return obj;
        }
    }

    class NamedItem : ContentNode
    {
        public override JToken ToJson()
        {
            JObject obj = new JObject();
            obj.Add("name", Name);
            return obj;
        }
    }

    abstract class ContentFile : ContentNode
    {
        public string Extension { get; set; }
    }

    class TextFile : ContentFile
    {
        public string Content { get; set; }

        public override JToken ToJson()
        {
            JObject obj = new JObject();
            obj.Add("name", Name);
            if (Extension != null)
            {
                obj.Add("extension", Extension);
            }
            if (Content != null)
            {
                obj.Add("content", Content);
            }
            return obj;
        }
    }

    class AssemblyFile : ContentFile
    {
        public ISet<string> Namespaces { get; private set; }
        public IList<string> Types { get; private set; }

        public AssemblyFile()
        {
            Namespaces = new HashSet<string>();
            Types = new List<string>();
        }

        public override JToken ToJson()
        {
            JArray namespaces = new JArray();
            foreach (string n in Namespaces)
            {
                namespaces.Add(n);
            }

            JArray types = new JArray();
            foreach (string t in Types)
            {
                types.Add(t);
            }

            JObject obj = new JObject();
            obj.Add("name", Name);
            obj.Add("namespaces", namespaces);
            obj.Add("types", types);
            return obj;
        }
    }

    class Contents
    {
        public ContentFolder Root { get; private set; }

        public Contents(IReadOnlyCollection<ZipArchiveEntry> entries)
        {
            IDictionary<string, ContentFolder> folders = new Dictionary<string, ContentFolder>();

            folders.Add(string.Empty, new ContentFolder { Name = string.Empty });

            foreach (ZipArchiveEntry entry in entries)
            {
                if (entry.Name == entry.FullName)
                {
                    folders[string.Empty].Children.Add(CreateFile(entry));
                }
                else
                {
                    string path = entry.FullName.Substring(0, entry.FullName.Length - entry.Name.Length);

                    path = path.TrimEnd('/');

                    string[] segments = path.Split('/');

                    string segment = string.Empty;

                    string prev;
                    for (int i = 0; i < segments.Length; i++)
                    {
                        segment = segments[i];

                        if (i == 0)
                        {
                            prev = string.Empty;
                        }
                        else
                        {
                            prev = segments[i - 1];
                        }

                        ContentFolder parent;
                        if (!folders.TryGetValue(prev, out parent))
                        {
                            parent = new ContentFolder { Name = prev };
                            folders.Add(prev, parent);
                        }

                        if (!folders.ContainsKey(segment))
                        {
                            ContentFolder child = new ContentFolder { Name = segment };
                            folders.Add(segment, child);
                            parent.Children.Add(child);
                        }
                    }

                    folders[segment].Children.Add(CreateFile(entry));
                }
            }

            Root = folders[string.Empty];
        }

        private static ContentNode CreateFile(ZipArchiveEntry entry)
        {
            int dotIndex = entry.Name.LastIndexOf('.');

            if (dotIndex != -1)
            {
                string extension = entry.Name.Substring(entry.Name.LastIndexOf('.'));

                if (extension.Length > 1)
                {
                    extension = extension.Substring(1).ToLowerInvariant();

                    if (extension == "xml" || extension == "nuspec" || extension == "txt" || extension == "ps1" || extension == "config")
                    {
                        string content = (new StreamReader(entry.Open())).ReadToEnd();
                        return new TextFile { Name = entry.Name, Extension = extension, Content = content };
                    }
                    else if (extension == "dll")
                    {
                        Stream stream = entry.Open();
                        byte[] rawAssembly = new byte[entry.Length];
                        for (int i = 0; i < entry.Length; i++)
                        {
                            rawAssembly[i] = (byte)stream.ReadByte();
                        }

                        try
                        {
                            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += new ResolveEventHandler(CurrentDomain_ReflectionOnlyAssemblyResolve); 
                            Assembly assembly = Assembly.ReflectionOnlyLoad(rawAssembly);

                            AssemblyFile file = new AssemblyFile { Name = entry.Name, Extension = extension };
                            foreach (Type type in assembly.GetTypes())
                            {
                                file.Namespaces.Add(type.Namespace);
                                file.Types.Add(type.FullName);
                            }
                            return file;
                        }
                        catch (Exception)
                        {
                            return new NamedItem { Name = entry.Name };
                        }
                    }
                }
            }

            return new NamedItem { Name = entry.Name };
        }

        private static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return System.Reflection.Assembly.ReflectionOnlyLoad(args.Name);
        }

        public void PrettyPrint(TextWriter writer)
        {
            Root.PrettyPrint(writer, 0);
        }

        public JToken ToJson()
        {
            return Root.ToJson();
        }
    }
}

