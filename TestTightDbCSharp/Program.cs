﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TightDb.TightDbCSharp;

[assembly: CLSCompliant(true)] //mark the public interface of this program as cls compliant (can be run from any .net language)
namespace TestTightDbCS
{
    using System.IO;
    using NUnit.Framework;
    using TightDb.TightDbCSharp.Extensions;
    using System.Globalization;
    using System.Reflection;


    [TestFixture]
    public static class EnvironmentTest
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "tightccs"), 
         System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String)"),
         System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ImageFileMachine"),
         System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "PeKind"),
         System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String,System.Object)")]
        [Test]
        public static void ShowVersionTest()
        {
            var PointerSize = IntPtr.Size;
            var VmBitness = (PointerSize == 8) ? "64bit" : "32bit";
            OperatingSystem os = Environment.OSVersion;
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            PortableExecutableKinds peKind;
            ImageFileMachine machine;
            executingAssembly.ManifestModule.GetPEKind(out peKind,out machine);
           // String thisapplocation = executingAssembly.Location;

            System.Console.WriteLine("Build number :              {0}", Program.buildnumber);
            System.Console.WriteLine("Pointer Size :              {0}", PointerSize);
            System.Console.WriteLine("Process Running as :        {0}", VmBitness);
            System.Console.WriteLine("Built as PeKind :           {0}", peKind);
            System.Console.WriteLine("Built as ImageFileMachine : {0}", machine);
            System.Console.WriteLine("OS Version :                {0}", os.Version.ToString());
            System.Console.WriteLine("OS Platform:                {0}", os.Platform.ToString());
            System.Console.WriteLine("");
            System.Console.WriteLine("Now Loading tight_c_cs.dll  - expecting it to be a "+VmBitness+" dll!");
            //System.Console.WriteLine("Loading "+thisapplocation+"...");


            using (Table t = new Table())
            {
                System.Console.WriteLine("C#  DLL        build number {0}",Table.GetDllVersionCSharp);
                System.Console.WriteLine("C++ DLL        build number {0}", Table.CPlusPlusLibraryVersion());
            }
            System.Console.WriteLine();
            System.Console.WriteLine();

        }
    }

    [TestFixture]
    public static class StringEncodingTest
    {
        //Right now this test uses creation of tables as a test - the column name will be set to all sorts of crazy thing, and we want them back that way
        [Test]
        public static void TableWithPerThousandSign()
        {
            String actualres;
            using (
            Table notSpecifyingFields = new Table(
                "subtable".Table()
                ))//at this point we have created a table with no fields
            {
                notSpecifyingFields.AddColumn(DataType.String, "12345‰7890");
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "table name is 12345 then the permille sign ISO 10646:8240 then 7890", notSpecifyingFields);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 2
Table Name  : table name is 12345 then the permille sign ISO 10646:8240 then 7890
------------------------------------------------------
 0      Table  subtable            
 1     String  12345‰7890          
------------------------------------------------------

";
            Assert.AreEqual(expectedres,actualres);        
        }
    



        [Test]
        public static void TableWithJapaneseCharacters()
        {
            String actualres;
            using (
            Table notSpecifyingFields = new Table(
                "subtable".Table()
                ))//at this point we have created a table with no fields
            {
                notSpecifyingFields.AddColumn(DataType.String,     "123\u70B9\u83DC678");
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "column name is 123 then two japanese characters then 678", notSpecifyingFields);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 2
Table Name  : column name is 123 then two japanese characters then 678
------------------------------------------------------
 0      Table  subtable            
 1     String  123"+"\u70B9\u83DC"+@"678            
------------------------------------------------------

";
            Assert.AreEqual( expectedres,actualres);        
        }
    }


    
    [TestFixture]
    public static class CreateTableTest
    {

        //test with the newest kind of field object constructores - lasse's inherited specialized ones

        [Test]
        public static void TypedFieldClasses()
        {
            String actualres;
            using (
            Table newFieldClasses = new Table(
                new StringField("F1"),
                new IntField("F2"),
                new SubTableField("Sub1",
                   new StringField("F11"),
                   new IntField("F12"))
            ))
            {
                newFieldClasses.AddColumn(DataType.String, "Buksestørrelse");
                
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "table created with all types using the new field classes", newFieldClasses);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 4
Table Name  : table created with all types using the new field classes
------------------------------------------------------
 0     String  F1                  
 1        Int  F2                  
 2      Table  Sub1                
    0     String  F11                 
    1        Int  F12                 
 3     String  Buksestørrelse      
------------------------------------------------------

";
            Assert.AreEqual(expectedres, actualres);
        }


        //illustration of field usage, usecase / unit test

        //The user can decide to create his own field types, that could then be used in several different table definitions, to ensure 
        //that certain kinds of fields used by common business logic always were of the correct type and setup
        //For example a field called itemcode that currently hold integers to denote owned item codes in a game,
        //but perhaps later should be a string field instead
        //if you have many IntegerField fields in many tables with item codes in them, you could use Itemcode instead, and then effect the change to string
        //only by changing the ineritance of the Itemcode type from IntegerField to StringField
        //thus by introducing your own class, You hide the field implementation detail from the users using this field type

        
        class ItemCode : IntField //whenever ItemCode is specified in a table definition, an IntegerField is created
        {
            public ItemCode(String columnName) : base(columnName) { }           
        }

        //because of a defense against circular field references, the subtablefield cannot be used this way, however you can make a method that returns an often
        //used subtable specification like this instead :

        //subtable field set used by our general login processing system
        public static SubTableField OwnedItems()
        {
            return new SubTableField(
                ("OwnedItems"),
                  new StringField("Item Name"),
                  new ItemCode("ItemId"),
                  new IntField("Number Owned"),
                  new BoolField("ItemPowerLevel"));
        }

        //game state dataset used by our general game saving system for casual games
        public static SubTableField GameSaveFields()
        {
            return new SubTableField(
                ("GameState"),
                  new StringField("SaveDate"),
                  new IntField("UserId"),
                  new StringField("Users description"),
                  new BinaryField("GameData1"),
                  new StringField("GameData2"));
        }


        //creation of table using user overridden or generated fields (ensuring same subtable structure across applications or tables)
        [Test]
        public static void UserCreatedFields()
        {
            String actualres;

            using (
            Table Game1 = new Table(
                OwnedItems(),
                new IntField("UserId"), //some game specific stuff. All players are owned by some item, don't ask me why
                new BinaryField("BoardLayout"), //game specific
                GameSaveFields())
                )
            {
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name+"1", "table created user defined types and methods", Game1);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 4
Table Name  : table created user defined types and methods
------------------------------------------------------
 0      Table  OwnedItems          
    0     String  Item Name           
    1        Int  ItemId              
    2        Int  Number Owned        
    3       Bool  ItemPowerLevel      
 1        Int  UserId              
 2     Binary  BoardLayout         
 3      Table  GameState           
    0     String  SaveDate            
    1        Int  UserId              
    2     String  Users description   
    3     Binary  GameData1           
    4     String  GameData2           
------------------------------------------------------

";
            Assert.AreEqual(expectedres, actualres);        




            using (Table Game2 = new Table(
            OwnedItems(),
            new ItemCode("UserId"), //game specific
            new ItemCode("UsersBestFriend"), //game specific
            new IntField("Game Character Type"), //game specific
            GameSaveFields()))
            {
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name + "2", "table created user defined types and methods", Game2);
            }
             expectedres =
@"------------------------------------------------------
Column count: 5
Table Name  : table created user defined types and methods
------------------------------------------------------
 0      Table  OwnedItems          
    0     String  Item Name           
    1        Int  ItemId              
    2        Int  Number Owned        
    3       Bool  ItemPowerLevel      
 1        Int  UserId              
 2        Int  UsersBestFriend     
 3        Int  Game Character Type 
 4      Table  GameState           
    0     String  SaveDate            
    1        Int  UserId              
    2     String  Users description   
    3     Binary  GameData1           
    4     String  GameData2           
------------------------------------------------------

";
            Assert.AreEqual(expectedres, actualres);        

        }
        

        //this kind of creation call should be legal - it creates a totally empty table, then only later sets up a field        
        [Test]
        public static void SubTableNoFields()
        {
            String actualres;
            using (
            Table notSpecifyingFields = new Table(
                "subtable".Table()
                ))//at this point we have created a table with no fields
            {
                notSpecifyingFields.AddColumn(DataType.String, "Buksestørrelse");
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name,"one field Created in two steps with table add column", notSpecifyingFields);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 2
Table Name  : one field Created in two steps with table add column
------------------------------------------------------
 0      Table  subtable            
 1     String  Buksestørrelse      
------------------------------------------------------

";
            Assert.AreEqual(expectedres, actualres);        
        }


        [Test]
        public static void TestHandleAcquireOneField()
        {
            string actualres;
            using (Table testtbl = new Table(new Field("name", DataType.String)))
            {
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "NameField", testtbl);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 1
Table Name  : NameField
------------------------------------------------------
 0     String  name                
------------------------------------------------------

";
            Assert.AreEqual(expectedres, actualres);
        }


        [Test]
        public static void TestHandleAcquireSeveralFields()
        {
            String actualres;
            using (Table testtbl3 = new Table(
            "Name".TightDbString(),
            "Age".TightDbInt(),
            "count".TightDbInt(),
            "Whatever".TightDbMixed()            
            ))
            {
                //long  test = testtbl3.getdllversion_CSH();
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "four columns, Last Mixed", testtbl3);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 4
Table Name  : four columns, Last Mixed
------------------------------------------------------
 0     String  Name                
 1        Int  Age                 
 2        Int  count               
 3      Mixed  Whatever            
------------------------------------------------------

";
            Assert.AreEqual(expectedres, actualres);
        }

        //test the alternative table dumper implementation that does not use table class
        [Test]
        public static void TestAllFieldTypesStringExtensions()
        {
            string actualres1;
            string actualres2;
            using (Table t = new Table(
               "Count".Int(),
               "Valid".Bool(),
               "Name".String(),
               "BLOB".Binary(),
               "Items".SubTable(
                   "ItemCount".Int(),
                   "ItemName".String()),
               "HtmlPage".Mixed(),
               "FirstSeen".Date(),
               "Fraction".Float(),
               "QuiteLargeNumber".Double()
        ))
            {
                actualres1 = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "Table with all allowed types (String Extensions)", t);
                actualres2 = Program.TableDumperSpec(MethodInfo.GetCurrentMethod().Name, "Table with all allowed types (String Extensions)", t);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 9
Table Name  : Table with all allowed types (String Extensions)
------------------------------------------------------
 0        Int  Count               
 1       Bool  Valid               
 2     String  Name                
 3     Binary  BLOB                
 4      Table  Items               
    0        Int  ItemCount           
    1     String  ItemName            
 5      Mixed  HtmlPage            
 6       Date  FirstSeen           
 7      Float  Fraction            
 8     Double  QuiteLargeNumber    
------------------------------------------------------

";
            Assert.AreEqual(expectedres, actualres1);
            Assert.AreEqual(expectedres, actualres2);
        }



        //test the alternative table dumper implementation that does not use table class
        [Test]
        public static void TestAllFieldTypesFieldClass()
        {
            string actualres1;
            string actualres2;
            using (Table t = new Table(
                     new Field("Count",DataType.Int),
                     new Field("Valid",DataType.Bool),
                     new Field("Name",DataType.String),
                     new Field("BLOB",DataType.Binary),
                     new Field("Items",
                          new Field("ItemCount",DataType.Int), 
                          new Field("ItemName",DataType.String)),        
                     new Field("HtmlPage", DataType.Mixed),
                     new Field("FirstSeen",DataType.Date),
                     new Field("Fraction",DataType.Float),
                     new Field("QuiteLargeNumber",DataType.Double)
        ))
            {
                actualres1 = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "Table with all allowed types (Field)", t);
                actualres2 = Program.TableDumperSpec(MethodInfo.GetCurrentMethod().Name, "Table with all allowed types (Field)", t);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 9
Table Name  : Table with all allowed types (Field)
------------------------------------------------------
 0        Int  Count               
 1       Bool  Valid               
 2     String  Name                
 3     Binary  BLOB                
 4      Table  Items               
    0        Int  ItemCount           
    1     String  ItemName            
 5      Mixed  HtmlPage            
 6       Date  FirstSeen           
 7      Float  Fraction            
 8     Double  QuiteLargeNumber    
------------------------------------------------------

";
            Assert.AreEqual(expectedres, actualres1);
            Assert.AreEqual(expectedres, actualres2);
        }



        //test the alternative table dumper implementation that does not use table class
        [Test]
        public static void TestAllFieldTypesTypedFields()
        {
            string actualres1;
            string actualres2;
            using (Table t = new Table(
                     new IntField("Count"),
                     new BoolField("Valid"),
                     new StringField("Name"),
                     new BinaryField("BLOB"),
                     new SubTableField("Items",
                          new IntField("ItemCount"),
                          new StringField("ItemName")),
                     new MixedField("HtmlPage"),
                     new DateField("FirstSeen"),
                     new FloatField("Fraction"),
                     new DoubleField("QuiteLargeNumber")
        ))
            {
                actualres1 = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "Table with all allowed types (Typed Field)", t);
                actualres2 = Program.TableDumperSpec(MethodInfo.GetCurrentMethod().Name, "Table with all allowed types (Typed Field)", t);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 9
Table Name  : Table with all allowed types (Typed Field)
------------------------------------------------------
 0        Int  Count               
 1       Bool  Valid               
 2     String  Name                
 3     Binary  BLOB                
 4      Table  Items               
    0        Int  ItemCount           
    1     String  ItemName            
 5      Mixed  HtmlPage            
 6       Date  FirstSeen           
 7      Float  Fraction            
 8     Double  QuiteLargeNumber    
------------------------------------------------------

";
            Assert.AreEqual(expectedres, actualres1);
            Assert.AreEqual(expectedres, actualres2);
        }


        //test with a subtable
        [Test]
        public static void TestMixedConstructorWithSubTables()
        {
            string actualres;
            using (
            Table testtbl = new Table(
                "Name".TightDbString(),
                "Age".TightDbInt(),
                new Field("age2", DataType.Int),
                new Field("age3", "Int"),
//                new IntegerField("Age3"),
                new Field("comments",
                              new Field("phone#1", DataType.String),
                              new Field("phone#2", DataType.String),
                              new Field("phone#3", "String"),
                              "phone#4".TightDbString()
                             ),
                new Field("whatever", DataType.Mixed)
                ))
            {
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "six colums,sub four columns", testtbl);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 6
Table Name  : six colums,sub four columns
------------------------------------------------------
 0     String  Name                
 1        Int  Age                 
 2        Int  age2                
 3        Int  age3                
 4      Table  comments            
    0     String  phone#1             
    1     String  phone#2             
    2     String  phone#3             
    3     String  phone#4             
 5      Mixed  whatever            
------------------------------------------------------

";
            Assert.AreEqual(expectedres, actualres);
        }




        [Test]
        //[NUnit.Framework.Ignore("Need to write tests that test for correct deallocation of table when out of scope")]
        //scope has been thoroughly debugged and does work perfectly in all imagined cases, but the testing was done before unit tests had been created
        public static void TestTableScope()
        {
            Table TestTable;//bad way to code this but i need the reference after the using clause
            using (TestTable = new Table())
            {

                Assert.False(TestTable.IsDisposed);//do a test to see that testtbl has a valid table handle 
            }
            Assert.True(TestTable.IsDisposed);
            //do a test here to see that testtbl now does not have a valid table handle


        }



        //while You cannot cross-link parents and subtables inside a new table() construct, you can try to do so, by deliberatly changing
        //the subtable references in Field objects that You instantiate yourself -and then call Table.create(Yourfiled) with a 
        //field definition that is self referencing.
        //however, currently this is not possible as seen in the example below.
        //the subtables cannot be changed directly, so all You can do is create new objects that has old already created objects as subtables
        //therefore a tree structure, no recursion.

        //below is my best shot at someone trying to create a table with custom built cross-linked field definitions (and failing)

        //I did not design the Field type to be used on its own like the many examples below. However , none of these weird uses break anything
        [Test]
        public static void TestIllegalFieldDefinitions1()
        {
            Field f5 = "f5".Int();//create a field reference, type does not matter
            f5 = "f5".Table(f5);//try to overwrite the field object with a new object that references itself 
            string actualres;
            using (
            Table t = new Table(f5))//this will not crash or loop forever the subtable field does not references itself 
            {
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "self-referencing subtable", t);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 1
Table Name  : self-referencing subtable
------------------------------------------------------
 0      Table  f5                  
    0        Int  f5                  
------------------------------------------------------

";
            Assert.AreEqual(expectedres, actualres);
        }

        [Test]
        public static void TestIllegalFieldDefinitions2()
        {
            Field fc = "fc".Int();//create a field reference, type does not matter
            Field fp = "fp".Table(fc);//let fp be the parent table subtable column, fc be the sole field in a subtable

            fc = "fc".Table(fp);//then change the field type from int to subtable and reference the parent

            //You now think You have illegal field definitions in fc and fp as both are subtables and both reference the other as the sole subtable field
            //however, they are new objects that reference the old classes that they replaced.
            String actualres;
            using (
                Table t2 = new Table(fc))
            { //should crash too
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "subtable that has subtable that references its parent #1", t2);
            }
            String Expectedres =
@"------------------------------------------------------
Column count: 1
Table Name  : subtable that has subtable that references its parent #1
------------------------------------------------------
 0      Table  fc                  
    0      Table  fp                  
       0        Int  fc                  
------------------------------------------------------

";


            Assert.AreEqual(Expectedres, actualres);
        }
        [Test]
        public static void TestIllegalFieldDefinitions3()
        {
            Field fc = "fc".Int();//create a field reference, type does not matter
            Field fp = "fp".Table(fc);//let fp be the parent table subtable column, fc be the sole field in a subtable
            fc = "fc".Table(fp);//then change the field type from int to subtable and reference the parent

            String actualres;
            using (
            Table t3 = new Table(fp))
            { //should crash too
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "subtable that has subtable that references its parent #2", t3);
            }
            String expectedres =
@"------------------------------------------------------
Column count: 1
Table Name  : subtable that has subtable that references its parent #2
------------------------------------------------------
 0      Table  fp                  
    0        Int  fc                  
------------------------------------------------------

";

            Assert.AreEqual(expectedres, actualres);

        }

        //super creative attemt at creating a cyclic graph of Field objects
        //still it fails because the array being manipulated is from GetSubTableArray and thus NOT the real list inside F1 even though the actual field objects referenced from the array ARE the real objects
        //point is - You cannot stuff field definitions down into the internal array this way
        [Test]
        public static void TestCyclicFieldDefinition1()
        {

            Field f1 = "f10".SubTable("f11".Int(), "f12".Int());
            var SubTableElements =  f1.GetSubTableArray();
            SubTableElements[0] = f1;//and the "f16" field in f1.f15.f16 is now replaced with f1.. recursiveness


            string actualres;
            using (Table t4 = new Table(f1))
            {
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "cyclic field definition", t4);
            }
            String expectedres =
@"------------------------------------------------------
Column count: 1
Table Name  : cyclic field definition
------------------------------------------------------
 0      Table  f10                 
    0        Int  f11                 
    1        Int  f12                 
------------------------------------------------------

";

            Assert.AreEqual(expectedres, actualres);
        }

        //dastardly creative terroristic attemt at creating a cyclic graph of Field objects
        //this creative approach succeeded in creating a stack overflow situation when the table is being created, but now it is not possible as AddSubTableFields has been made
        //internal, thus unavailable in customer assemblies.
        
        class TerroristField : Field
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "fielddefinitions"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "fieldName")]
            public  void setsubtablearray(String fieldName, Field[] fielddefinitions)//make the otherwise hidden addsubtablefield public
            {
//uncommenting the line below should create a compiletime error (does now) or else this unit test wil bomb the system
//                AddSubTableFields(this, fieldName,fielddefinitions);
            }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "columnName"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "subTableFieldsArray")]
            public TerroristField(string columnName, params Field[] subTableFieldsArray)
            {
                FieldType = DataType.Table;
            }
        }

        
        [Test]
        [ExpectedException("System.ArgumentNullException")]///while bombing the system with a circular field def is now not possible, we can always try to call in with null in a fieldname
        public static void TestCyclicFieldDefinition2()
        {

            TerroristField f1 = new TerroristField("f1",null);//do not care about last parameter we're trying to crash the system
            Field[] subs= new Field[2];
            subs[0]=f1;
            f1.setsubtablearray("f2", subs);

            string actualres;
            using (Table t4 = new Table(f1))
            {
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "cyclic field definition using field inheritance to get at subtable field list", t4);
            }
            String expectedres =
@"------------------------------------------------------
Column count: 1
Table Name  : cyclic field definition
------------------------------------------------------
 0      Table  f10                 
    0        Int  f11                 
    1        Int  f12                 
------------------------------------------------------

";

            Assert.AreEqual(expectedres, actualres);
        }






        [Test]
        public static void TestIllegalFieldDefinitions4()
        {

            Field f10 = "f10".SubTable("f11".Int(), "f12".Int());
            f10.FieldType = DataType.Int;
            //at this time, the subtable array still have some subtables in it
            string actualres;
            using (Table t4 = new Table(f10))
            {
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "just an int field, no subs", t4);
            }
            String expectedres =
@"------------------------------------------------------
Column count: 1
Table Name  : just an int field, no subs
------------------------------------------------------
 0        Int  f10                 
------------------------------------------------------

";

            Assert.AreEqual(expectedres, actualres);
        }
        [Test]
        public static void TestIllegalFieldDefinitions5()
        {
            Field f10 = "f10".SubTable("f11".Int(), "f12".Int());
            f10.FieldType = DataType.Table;

            String actualres;
            using (
         Table t5 = new Table(f10))
            {
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "subtable with two int fields", t5);//This is sort of okay, first adding a subtable, then
            }
            String expectedres =
@"------------------------------------------------------
Column count: 1
Table Name  : subtable with two int fields
------------------------------------------------------
 0      Table  f10                 
    0        Int  f11                 
    1        Int  f12                 
------------------------------------------------------

";
            Assert.AreEqual(expectedres, actualres);
            //changing mind and making it just and int field, and then changing mind again and setting it as subtable type
            //and thus resurfacing the two subfields. no harm done.
        }

        [Test]
        public static void TestCreateStrangeTable1()
        {
            //create a table with two columns that are the same name except casing (this might be perfectly legal, I dont know)
            String actualres;
            using (Table badtable = new Table("Age".Int(), "age".Int()))
            {
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "two fields, case is differnt", badtable);
            }
            String expectedres =
@"------------------------------------------------------
Column count: 2
Table Name  : two fields, case is differnt
------------------------------------------------------
 0        Int  Age                 
 1        Int  age                 
------------------------------------------------------

";
            Assert.AreEqual(expectedres,actualres);
        }
        [Test]
        public static void TestCreateStrangeTable2()
        {
            //Create a table with two columns with the same name and type
            String actualres;
            using (Table badtable2 = new Table("Age".Int(), "Age".Int()))
            {
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "two fields name and type the same", badtable2);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 2
Table Name  : two fields name and type the same
------------------------------------------------------
 0        Int  Age                 
 1        Int  Age                 
------------------------------------------------------

";
            Assert.AreEqual(expectedres, actualres);

        }


        //Test if two table creations where the second happens before the first is out of scope, works okay
        [Test]
        public static void TestCreateTwoTables()
        {
            StringBuilder actualres = new StringBuilder();//we add several table dumps into one compare string in this test
            using (
            Table testtbl1 = new Table(
            new Field("name", DataType.String),
            new Field("age", DataType.Int),
            new Field("comments",
                new Field("phone#1", DataType.String),
                new Field("phone#2", DataType.String)),
            new Field("whatever", DataType.Mixed)))
            {
                actualres.Append(Program.TableDumperSpec(MethodInfo.GetCurrentMethod().Name, "four columns , sub two columns (Field)", testtbl1));

                using (//and we create a second table while the first is in scope
                Table testtbl2 = new Table(
                    new Field("name", "String"),
                    new Field("age", "Int"),
                    new Field("comments",
                             new Field("phone#1", DataType.String),    //one way to declare a string
                             new Field("phone#2", "String"),           //another way
                             "more stuff".SubTable(
                                "stuff1".String(),                     //and yet another way
                                "stuff2".String(),
                                "ÆØÅæøå".String())
                             ),
                    new Field("whatever", DataType.Mixed)))
                {
                    actualres.Append(Program.TableDumperSpec(MethodInfo.GetCurrentMethod().Name, "four columns, sub three subsub three", testtbl2));
                }
            }
            File.WriteAllText(MethodInfo.GetCurrentMethod().Name + ".txt", actualres.ToString());
            string expectedres =
@"------------------------------------------------------
Column count: 4
Table Name  : four columns , sub two columns (Field)
------------------------------------------------------
 0     String  name                
 1        Int  age                 
 2      Table  comments            
    0     String  phone#1             
    1     String  phone#2             
 3      Mixed  whatever            
------------------------------------------------------

------------------------------------------------------
Column count: 4
Table Name  : four columns, sub three subsub three
------------------------------------------------------
 0     String  name                
 1        Int  age                 
 2      Table  comments            
    0     String  phone#1             
    1     String  phone#2             
    2      Table  more stuff          
       0     String  stuff1              
       1     String  stuff2              
       2     String  ÆØÅæøå              
 3      Mixed  whatever            
------------------------------------------------------

";
            Assert.AreEqual(expectedres,actualres.ToString());
        }

        [Test]
        public static void TestCreateStrangeTable3()
        {
            string actualres;
            using (
                Table Reallybadtable3 = new Table("Age".Int(),
                                                  "Age".Int(),
                                                  "".String(),
                                                  "".String()))
            {
                actualres = Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "same names int two empty string names", Reallybadtable3);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 4
Table Name  : same names int two empty string names
------------------------------------------------------
 0        Int  Age                 
 1        Int  Age                 
 2     String                      
 3     String                      
------------------------------------------------------

";
            Assert.AreEqual(expectedres,actualres);
        }

        [Test]
        public static void TestCreateStrangeTable4()
        {
            string actualres;
            using (
                Table Reallybadtable4 = new Table("Age".Int(),
                                      "Age".Mixed(),
                                      "".String(),
                                      "".Mixed()))
            {
                actualres=Program.TableDumper(MethodInfo.GetCurrentMethod().Name, "same names, empty names, mixed types", Reallybadtable4);
            }
            string expectedres =
@"------------------------------------------------------
Column count: 4
Table Name  : same names, empty names, mixed types
------------------------------------------------------
 0        Int  Age                 
 1      Mixed  Age                 
 2     String                      
 3      Mixed                      
------------------------------------------------------

";
            Assert.AreEqual(expectedres,actualres);
        }

    }



    class Program
    {

        public static int buildnumber = 1304041702;












        private static void printHeader(StringBuilder res, string tablename, long count)
        {
            res.AppendLine(headerline);
            res.AppendLine(String.Format(CultureInfo.InvariantCulture, "Column count: {0}", count));
            res.AppendLine(String.Format(CultureInfo.InvariantCulture, "Table Name  : {0}", tablename));
            res.AppendLine(headerline);            
        }


        private static void printFooter(StringBuilder res)
        {
            res.AppendLine(headerline);
            res.AppendLine("");
        }

        static string headerline = "------------------------------------------------------";


        //dumps table structure to a string for debugging purposes.
        //the string is easily human-readable
        //this version uses the table column information as far as possible, then shifts to spec on subtables
        public static string TableDumper(String fileName, String tableName, Table t)
        {
            StringBuilder res = new StringBuilder();//temporary storange of text of dump

            long count = t.ColumnCount;
            printHeader(res, tableName, count);
            for (long n = 0; n < count; n++)
            {
                string name = t.GetColumnName(n);
                DataType type = t.ColumnType(n);
                res.AppendLine(String.Format(CultureInfo.InvariantCulture, "{0,2} {2,10}  {1,-20}", n, name, type));
                if (type == DataType.Table)
                {                    
                    Spec subSpec =  t.Spec.GetSpec(n);
                    specdumper(res, "   ", subSpec, "Subtable");
                }
            }
            printFooter(res);
            System.Console.Write(res.ToString());
            File.WriteAllText(fileName + ".txt", res.ToString());
            return res.ToString();
        }


        private static void specdumper(StringBuilder res, String indent, Spec s, string TableName)
        {

            long count = s.ColumnCount;

            if (String.IsNullOrEmpty(indent))
            {
                printHeader(res, TableName, count);
            }

            for (long n = 0; n < count; n++)
            {
                String name = s.GetColumnName(n);
                DataType type = s.GetColumnType(n);
                res.AppendLine(String.Format(CultureInfo.InvariantCulture, "{0}{1,2} {2,10}  {3,-20}", indent, n, type, name));
                if (type == DataType.Table)
                {
                    Spec subspec = s.GetSpec(n);
                    specdumper(res, indent + "   ", subspec, "Subtable");
                }
            }

            if (String.IsNullOrEmpty(indent))
            {
                printFooter(res);
            }
        }

        //dump the table only using its spec
        public static String TableDumperSpec(String fileName, String tablename, Table t)
        {
            StringBuilder res = new StringBuilder();           
            specdumper(res, "", t.Spec, tablename);
            File.WriteAllText(fileName + ".txt", res.ToString());
            return res.ToString();
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String)")]
        static void Main(/*string[] args*/)
        {
            /*
             *  to debug unit tests, uncomment the lines below, and run the test(s) you want to have debugged
             *  remember to set a breakpoint
             *  Don't run the program in Nunit, simply debug it in visual studio when it runs like an ordinary program
             *  */

            EnvironmentTest.ShowVersionTest();
            CreateTableTest.UserCreatedFields();
            CreateTableTest.TypedFieldClasses();
          //  CreateTableTest.TestCyclicFieldDefinition2();///this should crash the program
            StringEncodingTest.TableWithPerThousandSign();
            StringEncodingTest.TableWithJapaneseCharacters();

            CreateTableTest.TestHandleAcquireOneField();

            CreateTableTest.TestHandleAcquireOneField();

            CreateTableTest.TestCreateTwoTables();
            CreateTableTest.TestTableScope();

            CreateTableTest.TestHandleAcquireSeveralFields();


            CreateTableTest.TestMixedConstructorWithSubTables();

            CreateTableTest.TestAllFieldTypesStringExtensions();

            CreateTableTest.TestIllegalFieldDefinitions1();
            CreateTableTest.TestIllegalFieldDefinitions2();
            CreateTableTest.TestIllegalFieldDefinitions3();
            CreateTableTest.TestIllegalFieldDefinitions4();
            CreateTableTest.TestIllegalFieldDefinitions5();

            
            System.Console.WriteLine("Press any key to finish test..");
            System.Console.ReadKey();
        }

    }
}
