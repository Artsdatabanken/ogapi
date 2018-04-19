using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Linq;

namespace Trees.Tests
{
    [TestClass]
    public class RTreeTests
    {
        private const string _jsonData =
 @"[[0,0,0,0],[10,10,10,10],[20,20,20,20],[25,0,25,0],[35,10,35,10],[45,20,45,20],[0,25,0,25],[10,35,10,35],
 [20, 45, 20, 45],[25, 25, 25, 25],[35, 35, 35, 35],[45, 45, 45, 45],[50, 0, 50, 0],[60, 10, 60, 10],[70, 20, 70, 20],[75, 0, 75, 0],
 [85, 10, 85, 10],[95, 20, 95, 20],[50, 25, 50, 25],[60, 35, 60, 35],[70, 45, 70, 45],[75, 25, 75, 25],[85, 35, 85, 35],[95, 45, 95, 45],
 [0, 50, 0, 50],[10, 60, 10, 60],[20, 70, 20, 70],[25, 50, 25, 50],[35, 60, 35, 60],[45, 70, 45, 70],[0, 75, 0, 75],[10, 85, 10, 85],
 [20, 95, 20, 95],[25, 75, 25, 75],[35, 85, 35, 85],[45, 95, 45, 95],[50, 50, 50, 50],[60, 60, 60, 60],[70, 70, 70, 70],[75, 50, 75, 50],
 [85, 60, 85, 60],[95, 70, 95, 70],[50, 75, 50, 75],[60, 85, 60, 85],[70, 95, 70, 95],[75, 75, 75, 75],[85, 85, 85, 85],[95, 95, 95, 95]]";

        private readonly RTreeNode<int>[] _data;

        public RTreeTests()
        {
            int id = 1;

            _data = JsonConvert.DeserializeObject<int[][]>(_jsonData)
                    .Select(array => new RTreeNode<int>(id++, new BoundingBox(array[0], array[1], array[2], array[3])))
                    .ToArray();
        }

        private static RTreeNode<int>[] SomeData(int n)
        {
            var data = new RTreeNode<int>[n];

            for (int i = 0; i < n; i++)
            {
                data[i] = new RTreeNode<int>(i, new BoundingBox(i, i, i, i));
            }

            return data;
        }

        [TestMethod]
        public void Constructor_Uses_Max_9_Entries_By_Default()
        {
            var treeWithDepthOfOne = new RTree<int>();
            treeWithDepthOfOne.Load(SomeData(9));
            Assert.AreEqual(1, treeWithDepthOfOne.Height);

            var treeWithDepthOfTwo = new RTree<int>();
            treeWithDepthOfTwo.Load(SomeData(10));
            Assert.AreEqual(2, treeWithDepthOfTwo.Height);
        }

        [TestMethod]
        public void Bulk_Load()
        {
            var tree = new RTree<int>(4);
            tree.Load(_data);

            CompareNodes(_data, tree.All().ToArray(), true);
        }

        private static void CompareNodes(RTreeNode<int>[] a, RTreeNode<int>[] b, bool compareDataItem = false)
        {
            Assert.AreEqual(a.Length, b.Length);

            var aOrdered = a
                        .OrderBy(x => x.BoundingBox.X1)
                        .ThenBy(x => x.BoundingBox.Y1)
                        .ThenBy(x => x.BoundingBox.X2)
                        .ThenBy(x => x.BoundingBox.Y2);

            var bOrdered = b
                        .OrderBy(x => x.BoundingBox.X1)
                        .ThenBy(x => x.BoundingBox.Y1)
                        .ThenBy(x => x.BoundingBox.X2)
                        .ThenBy(x => x.BoundingBox.Y2);

            if (compareDataItem)
            {
                aOrdered = aOrdered.ThenBy(x => x.Data);
                bOrdered = bOrdered.ThenBy(x => x.Data);
            }

            var aOrderedArray = aOrdered.ToArray();
            var bOrderedArray = bOrdered.ToArray();

            for (int i = 0; i < a.Length; i++)
            {
                if (compareDataItem)
                {
                    Assert.AreEqual(aOrderedArray[i].Data, bOrderedArray[i].Data);
                }

                Assert.AreEqual(aOrderedArray[i].BoundingBox.X1, bOrderedArray[i].BoundingBox.X1);
                Assert.AreEqual(aOrderedArray[i].BoundingBox.Y1, bOrderedArray[i].BoundingBox.Y1);
                Assert.AreEqual(aOrderedArray[i].BoundingBox.X2, bOrderedArray[i].BoundingBox.X2);
                Assert.AreEqual(aOrderedArray[i].BoundingBox.Y2, bOrderedArray[i].BoundingBox.Y2);
            }
        }

        [TestMethod]
        public void Load_Uses_Standard_Insertion_When_Given_Low_No_Of_Items()
        {
            var additionalNodes = new RTreeNode<int>[3];

            int id = _data.Last().Data + 1;

            for (int i = 0; i < additionalNodes.Length; i++)
            {
                additionalNodes[i] = new RTreeNode<int>(id, new BoundingBox(id, id, id, id));
                id++;
            }

            var tree = new RTree<int>(8);
            tree.Load(_data);
            tree.Load(additionalNodes);

            var tree2 = new RTree<int>(8);
            tree2.Load(_data);

            for (int i = 0; i < additionalNodes.Length; i++)
            {
                tree2.Insert(additionalNodes[i]);
            }

            string treeJson = tree.ToJson();
            string tree2Json = tree2.ToJson();

            Assert.AreEqual(treeJson, tree2Json);
        }

        [TestMethod]
        public void Load_Properly_Splits_Tree_Root_When_Merging_Trees_Of_Same_Height()
        {
            var tree = new RTree<int>(4);
            tree.Load(_data);
            tree.Load(_data);

            Assert.AreEqual(4, tree.Height);

            CompareNodes(_data.Concat(_data).ToArray(), tree.All().ToArray(), true);
        }

        [TestMethod]
        public void Load_Properly_Merges_Data_Of_Smaller_Or_Bigger_Tree_Heights()
        {
            var smaller = SomeData(10);

            var tree1 = new RTree<int>(4);
            tree1.Load(_data);
            tree1.Load(smaller);

            var tree2 = new RTree<int>(4);
            tree2.Load(smaller);
            tree2.Load(_data);

            Assert.AreEqual(tree1.Height, tree2.Height);

            CompareNodes(_data.Concat(smaller).ToArray(), tree1.All().ToArray());
            CompareNodes(_data.Concat(smaller).ToArray(), tree2.All().ToArray());
        }

        [TestMethod]
        public void Search_Finds_Matching_Points_Given_Bbox()
        {
            var tree = new RTree<int>(4);
            tree.Load(_data);

            var result = tree.Search(new BoundingBox(40, 20, 80, 70));
            const string expectedResultAsJson =
                @"[[70,20,70,20],[75,25,75,25],[45,45,45,45],[50,50,50,50],[60,60,60,60],[70,70,70,70],
                [45,20,45,20],[45,70,45,70],[75,50,75,50],[50,25,50,25],[60,35,60,35],[70,45,70,45]]";

            int id = -1;
            var expectedResult = JsonConvert.DeserializeObject<int[][]>(expectedResultAsJson).Select(array => new RTreeNode<int>(id, new BoundingBox(array[0], array[1], array[2], array[3]))).ToArray();

            CompareNodes(expectedResult, result.ToArray());
        }

        [TestMethod]
        public void Collides_When_Search_Finds_Matching_Points()
        {
            var tree = new RTree<int>(4);
            tree.Load(_data);

            Assert.IsTrue(tree.Collides(new BoundingBox(40, 20, 80, 70)));
        }

        [TestMethod]
        public void Search_Returns_Empty_Enumerable_If_Nothing_Found()
        {
            var tree = new RTree<int>(4);
            tree.Load(_data);

            var result = tree.Search(new BoundingBox(200, 200, 210, 210)).ToList();

            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void Collides_Returns_False_If_Nothing_Found()
        {
            var tree = new RTree<int>(4);
            tree.Load(_data);

            Assert.IsFalse(tree.Collides(new BoundingBox(200, 200, 210, 210)));
        }

        [TestMethod]
        public void All_Returns_All_Points_In_The_Tree()
        {
            var tree = new RTree<int>(4);
            tree.Load(_data);

            var all = tree.All();

            CompareNodes(_data, all.ToArray(), true);
        }

        [TestMethod]
        public void ToJson_And_FromJson_Exports_And_Imports_Tree()
        {
            var tree = new RTree<int>(4);
            tree.Load(_data);

            string json = tree.ToJson();
            var treeFromJson = new RTree<int>(4);
            treeFromJson.FromJson(json);

            CompareNodes(tree.All().ToArray(), treeFromJson.All().ToArray());
        }

        [TestMethod]
        public void Insert_Adds_An_Item_To_An_Existing_Tree_Correctly()
        {
            const string itemsAsJson =
                @"[
                    [0, 0, 0, 0],
                    [1, 1, 1, 1],
                    [2, 2, 2, 2],
                    [3, 3, 3, 3],
                    [1, 1, 2, 2]
                ]";

            int id = -1;
            var items = JsonConvert.DeserializeObject<int[][]>(itemsAsJson).Select(array => new RTreeNode<int>(id, new BoundingBox(array[0], array[1], array[2], array[3]))).ToArray();

            var tree = new RTree<int>(4);
            tree.Load(items.Take(3));

            tree.Insert(items[3]);

            Assert.AreEqual(1, tree.Height);

            tree.Insert(items[4]);

            Assert.AreEqual(2, tree.Height);

            CompareNodes(items, tree.All().ToArray(), true);
        }

        [TestMethod]
        public void Insert_Forms_A_Valid_Tree_If_Items_Are_Inserted_One_By_One()
        {
            var tree = new RTree<int>(4);

            foreach (var item in _data)
            {
                tree.Insert(item);
            }

            var tree2 = new RTree<int>(4);
            tree2.Load(_data);

            Assert.IsTrue(tree.Height - tree2.Height <= 1);

            CompareNodes(tree.All().ToArray(), tree2.All().ToArray());
        }

        [TestMethod]
        public void Remove_Removes_Items_Correctly()
        {
            var tree = new RTree<int>(4);
            tree.Load(_data);

            var length = _data.Length;

            tree.Remove(_data[0]);
            tree.Remove(_data[1]);
            tree.Remove(_data[2]);

            tree.Remove(_data[length - 1]);
            tree.Remove(_data[length - 2]);
            tree.Remove(_data[length - 3]);

            var dataRemainng = _data.Skip(3).Take(length - 6).ToArray();

            CompareNodes(dataRemainng, tree.All().ToArray(), true);
        }

        [TestMethod]
        public void Clear_Should_Reset_The_Tree()
        {
            var tree = new RTree<int>(4);
            tree.Load(_data);

            tree.Clear();

            var tree2 = new RTree<int>(4);

            Assert.AreEqual(tree.ToJson(), tree2.ToJson());
        }

        [TestMethod]
        public void Chainable_Api()
        {
            var all = new RTree<int>().Load(_data).Insert(_data[0]).Remove(_data[0]).All();

            CompareNodes(_data.Skip(1).ToArray(), all.ToArray(), true);
        }
    }
}