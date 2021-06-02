using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using SourceExpander;
using Xunit;

namespace StandaloneSourceExpander.Test
{
    public class ExpandingProjectTest
    {
        public ExpandingProjectTest()
        {
            var fi = new FileInfo(CurrentPath());
            sampleCsProj = fi.Directory.Parent.Parent
                .EnumerateFiles("Source/Sandbox/SampleApp/SampleApp.csproj").First();
            sampleCsProj.Exists.Should().BeTrue();
        }

        readonly FileInfo sampleCsProj;

        [Fact]
        public async Task ExpandAsync()
        {
            var expanded = await new ExpandingProject(sampleCsProj.FullName).ExpandAsync();
            expanded.Should().Contain(t => t.filePath.Contains("UnionFind") && ExpandedIsUnionFind(t.expandedCode));
        }
        static bool ExpandedIsUnionFind(string code) =>
            code.Replace("\r", "") == @"using AtCoder;
using AtCoder.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace SampleLibrary
{
    public class UnionFind : DSU
    {
        public UnionFind(int n) : base(n) { }
    }
}
#region Expanded by https://github.com/naminodarie/SourceExpander
namespace AtCoder{public class DSU{internal readonly int _n;public readonly int[]_parentOrSize;public DSU(int n){_n=n;_parentOrSize=new int[n];_parentOrSize.AsSpan().Fill(-1);}public int Merge(int a,int b){int x=Leader(a),y=Leader(b);if(x==y)return x;if(-_parentOrSize[x]<-_parentOrSize[y])(x,y)=(y,x);_parentOrSize[x]+=_parentOrSize[y];_parentOrSize[y]=x;return x;}public bool Same(int a,int b){return Leader(a)==Leader(b);}public int Leader(int a){if(_parentOrSize[a]<0)return a;while(0<=_parentOrSize[_parentOrSize[a]]){(a,_parentOrSize[a])=(_parentOrSize[a],_parentOrSize[_parentOrSize[a]]);}return _parentOrSize[a];}public int Size(int a){return-_parentOrSize[Leader(a)];}public int[][]Groups(){int[]leaderBuf=new int[_n];int[]id=new int[_n];var resultList=new SimpleList<int[]>(_n);for(int i=0;i<leaderBuf.Length;i++){leaderBuf[i]=Leader(i);if(i==leaderBuf[i]){id[i]=resultList.Count;resultList.Add(new int[-_parentOrSize[i]]);}}var result=resultList.ToArray();int[]ind=new int[result.Length];for(int i=0;i<leaderBuf.Length;i++){var leaderID=id[leaderBuf[i]];result[leaderID][ind[leaderID]]=i;ind[leaderID]++;}return result;}}}
namespace AtCoder.Internal{public class SimpleList<T>:IList<T>,IReadOnlyList<T>{private T[]data;private const int DefaultCapacity=2;public SimpleList(){data=new T[DefaultCapacity];}public SimpleList(int capacity){data=new T[Math.Max(capacity,DefaultCapacity)];}public SimpleList(IEnumerable<T>collection){if(collection is ICollection<T>col){data=new T[col.Count];col.CopyTo(data,0);Count=col.Count;}else{data=new T[DefaultCapacity];foreach(var item in collection)Add(item);}}public Memory<T>AsMemory()=>new Memory<T>(data,0,Count);public Span<T>AsSpan()=>new Span<T>(data,0,Count);public ref T this[int index]{[MethodImpl(MethodImplOptions.AggressiveInlining)]get{if((uint)index>=(uint)Count)ThrowIndexOutOfRangeException();return ref data[index];}}public int Count{get;private set;}public void Add(T item){if((uint)Count>=(uint)data.Length)Array.Resize(ref data,data.Length<<1);data[Count++]=item;}public void RemoveLast(){if( --Count<0)ThrowIndexOutOfRangeException();}public SimpleList<T>Reverse(){Array.Reverse(data,0,Count);return this;}public SimpleList<T>Reverse(int index,int count){Array.Reverse(data,index,count);return this;}public SimpleList<T>Sort(){Array.Sort(data,0,Count);return this;}public SimpleList<T>Sort(IComparer<T>comparer){Array.Sort(data,0,Count,comparer);return this;}public SimpleList<T>Sort(int index,int count,IComparer<T>comparer){Array.Sort(data,index,count,comparer);return this;}public void Clear()=>Count=0;public bool Contains(T item)=>IndexOf(item)>=0;public int IndexOf(T item)=>Array.IndexOf(data,item,0,Count);public void CopyTo(T[]array,int arrayIndex)=>Array.Copy(data,0,array,arrayIndex,Count);public T[]ToArray()=>AsSpan().ToArray();bool ICollection<T>.IsReadOnly=>false;T IList<T>.this[int index]{get=>data[index];set=>data[index]=value;}T IReadOnlyList<T>.this[int index]{get=>data[index];}void IList<T>.Insert(int index,T item)=>throw new NotSupportedException();bool ICollection<T>.Remove(T item)=>throw new NotSupportedException();void IList<T>.RemoveAt(int index)=>throw new NotSupportedException();IEnumerator IEnumerable.GetEnumerator()=>((IEnumerable<T>)this).GetEnumerator();IEnumerator<T>IEnumerable<T>.GetEnumerator(){for(int i=0;i<Count;i++)yield return data[i];}public Span<T>.Enumerator GetEnumerator()=>AsSpan().GetEnumerator();private static void ThrowIndexOutOfRangeException()=>throw new IndexOutOfRangeException();}}
#endregion Expanded by https://github.com/naminodarie/SourceExpander
".Replace("\r", "");
        static string CurrentPath([CallerFilePath] string path = "") => path;
    }
}
