using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using MySql.Data.MySqlClient;
using MongoDB.Driver;

namespace txtGenerator
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            /// Vi har observerat att första gången som dokumenten läses in så tar det längre tid än senare, 
            /// att skapa och lagra 10 objekt tog 10 gånger längre tid än att skapa och lagra 10000 objekt, 
            /// Därför testar vi att använda denna funktion för att läsa in dokumenten i RAMet som är betydligt mycket snabbare
            /// än hårddisken.
            /// 
            /// Eftersom att dessa funktioner inte hjälpte kan det vara så att operativsystemet måste öppna en "skriv" process
            /// denna process tar lite tid att öppnas, men stannar sedan öppen under hela applikationens gång. 
            /// Detta kan vara varför första skrivningen alltid tar längre tid än senare.

            IMongoDatabase mongoDatabase = CreateMongoDBDatabase();

            Console.WriteLine("För att använda detta program behöver du ha MongoDB installerat och en SQL-server aktiverad." +
                "\nVid testning användes MariaDB via XAMPP för SQL; https://www.apachefriends.org/index.html" +
                "\nFör att kunna kommunicera med SQL-databasen användes MySQL Connector: https://dev.mysql.com/downloads/connector/net/" +
                "\nOch MongoDB Community Server för MongoDB; https://www.mongodb.com/download-center/community " +
                "\nFör att kunna kommunicera med MongoDB-databasen användes MongoDB C#/.NET Driver: https://docs.mongodb.com/ecosystem/drivers/csharp/ \n");
             
            Char choice;
            do
            {
                Console.Write("\nVarje separat program kommer att läsa in ett TXT dokument, lagra det i respektive databas," +
                    "\nändra på filen och ta tid på varje segment. Varje lagringssätt kommer testas på 4 olika mängder objekt:" +
                    "\n10, 100, 1000 och 10000 objekt kommer användas." +
                    "\nDetta program avser testa CRUD (Create, Read, Update, Delete) med avseende på två databaser, mySQL och MongoDB." +
                    "\n" +
                    "\nMeny:" +
                    "\n1: Skapa TXT dokument" +
                    "\n2: Fyll MongoDB" +
                    "\n3: Läs in objekt från MongoDB" +
                    "\n4: Updatera MongoDB" +
                    "\n5: Ta bort 1 random objekt från MongoDB" +
                    "\n6: Fyll SQL" +
                    "\n7: Läs in objekt från SQL" +
                    "\n8: Updatera SQL" +
                    "\n9: Ta bort 1 random objekt från SQL" +
                    "\n0: Rensa Databaser" +
                    "\ne: Avsluta program" +
                    "\nSvar: ");
                choice = Console.ReadKey().KeyChar;
                Console.WriteLine();
                // Skapar en array med antalet objekt som skall skapas
                int[] starAmount = { 10, 100, 1000, 10000 };
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Reset();


                switch (choice)
                {
                    case '1':
                        // Ankallar GenerateTXT() metoden och skapar 4 olika dokument med 10, 100, 1000 respektive 10000 objekt, enligt starAmount-arrayen.
                        // Randomiser
                        Random rnd = new Random();
                        foreach (int i in starAmount)
                            GenerateTXT(String.Format("TXT{0}.txt", i), i, rnd);
                        break;

                    case '2':
                        RecreateMongoDB(mongoDatabase);
                        foreach (int i in starAmount)
                        {
                            IMongoCollection<Star> collection = CreateMongoDBCollection(mongoDatabase, i);
                            InsertMongoDB(String.Format("TXT{0}.txt", i), collection, i);
                        }
                        break;

                    case '3':
                        ReadMongoDB(mongoDatabase);
                        break;

                    case '4':
                        stopwatch.Start();
                        foreach (int i in starAmount)
                        {
                            IMongoCollection<Star> collection = CreateMongoDBCollection(mongoDatabase, i);
                            UpdateMongoDB(i, 10, 5, collection);
                            UpdateMongoDB(i, 50, 5, collection);
                            UpdateMongoDB(i, 90, 5, collection);
                        }
                        Console.WriteLine("Total tid att ändra {0} MongoDB-databaser:                   {1}", starAmount.Length, stopwatch.Elapsed);
                        break;

                    case '5':
                        foreach (int i in starAmount)
                        {
                            IMongoCollection<Star> collection = CreateMongoDBCollection(mongoDatabase, i);
                            DeleteMongoDB(i, collection);
                        }
                        break;

                    case '6':
                        foreach (int i in starAmount)
                            InsertSQL(String.Format("TXT{0}.txt", i), i);
                        break;

                    case '7':
                        ReadSQL();
                        break;

                    case '8':
                        stopwatch.Start();
                        foreach (int i in starAmount)
                        {
                            UpdateSQL(i, 10, 5);
                            UpdateSQL(i, 50, 5);
                            UpdateSQL(i, 90, 5);
                        }
                        Console.WriteLine("Total tid att ändra {0} SQL-databaser:                   {1}", starAmount.Length, stopwatch.Elapsed);
                        break;

                    case '9':
                        foreach (int i in starAmount)
                            DeleteSQL(i, null);
                        break;
                    case '0':
                        mongoDatabase = RecreateMongoDB(mongoDatabase);
                        Console.WriteLine();
                        CreateSQLDatabase(starAmount);
                        break;

                    default:
                        // Error Handling
                        // Alla andra knapptryckningar k
                        break;
                }
            }
            while (choice != 'e');
        }


        public static Star RandomStar(Random rnd, int id)
        {
            // Ett färdigt Random-objekt inhämtas för att minska prestandakostnaden
            // Om man nyskapar Random-objektet flera tusen gånger per sekund kommer flera av dem att ha samma "seed",
            // detta kommer orsaka problem eftersom då kommer flera objekt med likadana värden att skapas

            // Skapar ett nytt stjärnobjekt
            Star star = new Star();

            // Ger varje stjärna ett unikt ID
            star._id = id;
            // Randomiserar stjärnans X position
            star.PosX = rnd.Next(1, 2561);
            // Randomiserar stjärnans Y position
            star.PosY = rnd.Next(1, 1601);
            // Randomiserar stjärnans radie
            star.Radius = rnd.Next(1, 11);
            // Randomiserar stjärnans ålder
            star.Age = rnd.Next();
            // Convert.ToChar(int) använder int som ett ascii-värde
            star.Activity = Convert.ToChar(rnd.Next(0, 10) + 97);
            // .ToString("X6") gör om ints till ett hexadecimalt värde med 6 platser och stora bokstäver
            star.Colour = rnd.Next(16777215).ToString("X6");

            return star;
        }

        /// <summary>
        /// Generate TXT file with starsAmount amount of stars
        /// </summary>
        public static void GenerateTXT(string fileName, int starAmount, Random rnd)
        {
            int amount = 0;

            // Skapar lista
            List<Star> starList = new List<Star>();

            Stopwatch oldTime = new Stopwatch();
            oldTime.Start();
            for (int i = 0; i < starAmount; i++)
            {
                // Lägger till stjärnan i listan
                starList.Add(RandomStar(rnd, i + 1));

                amount += 1;
            }

            Console.WriteLine("Tid att generera {0} objekt:                                    {1}", starAmount, oldTime.Elapsed);
            oldTime.Restart();
            StreamWriter sw = new StreamWriter(fileName, false);
            foreach (Star s in starList)
                sw.WriteLine(s.ToString());
            sw.Close();
            Console.WriteLine("Tid att skriva objekten till fil:                               {0}", oldTime.Elapsed);
        }

        /// <summary>
        /// Utdaterad. Integrerad in i CreateSQLDatabase()
        /// </summary>
        /// <param name="starAmount"></param>
        public static void CreateSQLTable(int starAmount)
        {
            String connectionString = @"Server=localhost;userid=root;password=;Database=stars;";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                String tableString = String.Format("CREATE TABLE stars{0} ( id int NOT NULL AUTO_INCREMENT, posX int(4) NOT NULL, posY int(4) NOT NULL, radius int(2) NOT NULL, age int(10) NOT NULL, activity char NOT NULL, colour varchar(6) NOT NULL, CONSTRAINT PRIMARY KEY (id)) ENGINE = MyISAM;", starAmount);
                MySqlCommand tableCommand = new MySqlCommand(tableString, connection);
                tableCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void CreateSQLDatabase(int[] starAmount)
        {
            String connectionString = @"Server=localhost;userid=root;password=;";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    // SQL kod för att ta bort databasen "stars" om den finns.
                    String dropString = "drop database stars;";
                    MySqlCommand dropCommand = new MySqlCommand(dropString, connection);
                    dropCommand.ExecuteNonQuery();
                    Console.WriteLine("En SQL-databas 'stars' har raderats.");


                    // SQL kod som skapar databasen 'stars'
                    String createString = "create database stars;";
                    MySqlCommand createCommand = new MySqlCommand(createString, connection);
                    createCommand.ExecuteNonQuery();

                    // SQL kod som säger att stars nu kommer användas
                    String useString = "use Stars;";
                    MySqlCommand useCommand = new MySqlCommand(useString, connection);
                    useCommand.ExecuteNonQuery();

                    foreach (int i in starAmount)
                    {
                        String tableString = String.Format("CREATE TABLE stars{0} (id int NOT NULL AUTO_INCREMENT, posX int(4) NOT NULL, posY int(4) NOT NULL, radius int(2) NOT NULL, age int(10) NOT NULL, activity char NOT NULL, colour varchar(6) NOT NULL, CONSTRAINT PRIMARY KEY (id)) ENGINE = MyISAM;", i);
                        MySqlCommand tableCommand = new MySqlCommand(tableString, connection);
                        tableCommand.ExecuteNonQuery();
                    }

                    Console.WriteLine("En SQL-databas 'stars' har blivit skapad och är nu i bruk.");
                }

                catch (MySqlException MSX)
                {
                    Console.WriteLine("Du verkar inte ha startat en mySQL-server, vänligen starta en mySQL-server och var säker på att du har root-access.");
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="starAmount"></param>
        public static void InsertSQL(String filename, int starAmount)
        {
            // https://www.codeproject.com/Tips/226720/Converting-a-List-to-a-DataTable
            // https://docs.microsoft.com/en-us/dotnet/api/system.data.datatable?view=netframework-4.7.2
            List<Star> stars = Star.ToStar(filename);
            Stopwatch oldTime = new Stopwatch();
            oldTime.Start();

            String connectionString = @"Server=localhost;userid=root;password=;Database=stars;";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                String truncString = String.Format("TRUNCATE TABLE stars.stars{0};", starAmount);
                MySqlCommand truncCommand = new MySqlCommand(truncString, connection);
                truncCommand.ExecuteNonQuery();
                foreach (Star s in stars)
                {
                    String queryString = String.Format("INSERT INTO stars.stars{0} (id, posX, posY, radius, age, activity, colour) " +
                        "VALUES (NULL, {1}, {2}, {3}, {4}, '{5}', '{6}');", starAmount, s.PosX, s.PosY, s.Radius, s.Age, s.Activity, s.Colour);

                    MySqlCommand sqlCommand = new MySqlCommand(queryString, connection);
                    sqlCommand.ExecuteNonQuery();
                }
                connection.Close();
            }
            Console.WriteLine("Tid att lagra {0} stjärnor till SQL-Databas:                        {1}\n", starAmount, oldTime.Elapsed);
        }


        public static void ReadSQL()
        {
            Char choice = '0';
            int collectionIndex = 0;
            int starIndex;

            String connectionString = @"Server=localhost;userid=root;password=;Database=stars;";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                do
                {
                    Console.WriteLine("\nVälj en av följande tillgängliga tabeller (a avbryter): ");
                    // Detta SQL kommando kommer returnera alla tabellnamn som finns i stars-databasen
                    String getTableString = "SELECT table_name FROM information_schema.tables WHERE table_schema = \"stars\";";
                    MySqlCommand getTableCommand = new MySqlCommand(getTableString, connection);
                    // .ExecuteReader() returnerar ett DataReader objekt baserat på SQL kommandot ovanför

                    using (var collections = getTableCommand.ExecuteReader())
                    {
                        foreach (var document in collections)
                        {
                            collectionIndex += 1;
                            Console.WriteLine("{0}: {1}", collectionIndex, collections.GetTextReader(0).ReadLine());
                        }
                    }
                    choice = Console.ReadKey().KeyChar;
                    Console.WriteLine(choice);


                    if (choice == 'a') ;
                    else
                    {
                        try
                        {
                            collectionIndex = Convert.ToInt32(choice) - 48;
                            Console.WriteLine("\n\nVänligen ange indexet på stjärnan du skulle vilja förhandsgranska: ");
                            starIndex = Convert.ToInt32(Console.ReadLine());

                            Stopwatch oldTime = new Stopwatch();
                            oldTime.Start();
                            String getSpecificString = String.Format("SELECT * FROM stars{0} WHERE id = {1};", Math.Pow(10, collectionIndex), starIndex);
                            MySqlCommand getSpecificCommand = new MySqlCommand(getSpecificString, connection);
                            using (var reader = getSpecificCommand.ExecuteReader())
                            {
                                while (reader.Read())
                                    Console.WriteLine("ID | PosX | PosY | Radius | Colour | Activity | Age");
                                    Console.WriteLine("{0}|{1}|{2}|{3}|{4}|{5}|{6}", reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3), reader.GetString(6), reader.GetChar(5), reader.GetInt32(4));
                            }
                            oldTime.Stop();
                            Console.WriteLine("Total tid att söka upp 1 objekt:                             {0}\n", oldTime.Elapsed);
                        }

                        catch (Exception e)
                        {
                            if (choice == 'a')
                                Console.WriteLine("Du har valt att inte förhandsgranska några element.");
                            else
                                Console.WriteLine("Du angav ett felaktigt alternativ, vänligen försök igen.");
                        }
                    }
                    Console.WriteLine(choice);
                }
                while (choice != 'a');
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="starAmount"></param>
        /// <param name="percentage"></param>
        /// <param name="repeats"></param>
        public static void UpdateSQL(int starAmount, int percentage, int repeats)
        {
            Random rnd = new Random();
            List<int> totalChanged = new List<int>();
            List<TimeSpan> timeAverage = new List<TimeSpan>();
            Stopwatch oldTime = new Stopwatch();
            Stopwatch oldTimeTotal = new Stopwatch();
            oldTimeTotal.Start();

            int changedAmount = 0;
            StreamWriter sw = new StreamWriter(string.Format("SQL_Data{0}_{1}%.txt", starAmount, percentage));
            sw.Close();

            for (int i = 0; i < repeats; i++)
            {
                // Resettar 
                changedAmount = 0;
                oldTime.Reset();
                oldTime.Start();

                String connectionString = @"Server=localhost;userid=root;password=;Database=stars;";

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    for (int u = 0; u < starAmount; u++)
                    {
                        int randInt = rnd.Next();

                        if (randInt % 100 < percentage)
                        {
                            changedAmount += 1;

                            Star s = RandomStar(rnd, u + 1);

                            String starString = String.Format("UPDATE stars{0} SET posX = {1}, posY = {2}, radius = {3}, age = {4}, activity = '{5}', colour = '{6}' WHERE id = {7};", starAmount, s.PosX, s.PosY, s.Radius, s.Age, s.Activity, s.Colour, u + 1);
                            MySqlCommand starCommand = new MySqlCommand(starString, connection);
                            starCommand.ExecuteNonQuery();
                        }
                    }
                    // Stoppar klockan här så att öppningen av ett nytt StreamWriter-objekt inte påverkar tiden
                    oldTime.Stop();

                    sw = new StreamWriter(string.Format("SQL_Data{0}_{1}%.txt", starAmount, percentage), true);
                    sw.WriteLine("{0}|{1}", changedAmount, oldTime.Elapsed);
                    sw.Close();
                    totalChanged.Add(changedAmount);
                    timeAverage.Add(oldTime.Elapsed);

                    connection.Close();
                }

            }
            oldTimeTotal.Stop();
            int changed = 0;
            foreach (int i in totalChanged)
                changed += i;

            TimeSpan time = new TimeSpan();
            foreach (TimeSpan t in timeAverage)
                time += t;

            Console.WriteLine("Genomsnittlig tid att ändra {0} av {1} objekt i SQL:                     {2}", changed / repeats, starAmount, new TimeSpan(time.Ticks / repeats));
            Console.WriteLine("Total tid att ändra i genomsnitt {0} objekt {1} gånger:                  {2}\n", changed / repeats, repeats, oldTimeTotal.Elapsed);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stars"></param>
        /// <param name="starAmount"></param>
        /// <returns></returns>
        public static void DeleteSQL(int starAmount, String fileType)
        {
            Random rnd = new Random();
            int id = rnd.Next(1, starAmount + 1);
            Stopwatch oldTime = new Stopwatch();
            oldTime.Start();
            Console.WriteLine("Tar bort stjärnan med id: {0}", id);

            String connectionString = @"Server=localhost;userid=root;password=;Database=stars;";
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                String starString = String.Format("DELETE FROM stars{0} WHERE id = {1};", starAmount, id);
                MySqlCommand starCommand = new MySqlCommand(starString, connection);
                starCommand.ExecuteNonQuery();
                Console.WriteLine("Tid att ta bort 1 objekt från SQL-databasen: {0}", oldTime.Elapsed);
                connection.Close();
            }
        }


        public static IMongoDatabase RecreateMongoDB(IMongoDatabase database)
        {
            database.Client.DropDatabase("Stars");
            Console.WriteLine("En MongoDB-databas 'Stars' har raderats.");

            // Återskapar databasen
            MongoClient client = new MongoClient("mongodb://localhost:27017");
            Console.WriteLine("En MongoDB-databas 'Stars' har blivit skapad och är nu i bruk.");
            return client.GetDatabase("Stars");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="starAmount"></param>
        public static IMongoDatabase CreateMongoDBDatabase()
        {
            MongoClient client = new MongoClient("mongodb://localhost:27017");
            var database = client.GetDatabase("Stars");
            return database;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="database"></param>
        /// <param name="starAmount"></param>
        /// <returns></returns>
        public static IMongoCollection<Star> CreateMongoDBCollection(IMongoDatabase database, int starAmount)
        {
            var collection = database.GetCollection<Star>(String.Format("stars{0}", starAmount));
            return collection;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="starAmount"></param>
        /// <param name="collection"></param>
        public static void InsertMongoDB(String filename, IMongoCollection<Star> collection, int starAmount)
        {
            Stopwatch oldTime = new Stopwatch();
            oldTime.Start();
            List<Star> stars = Star.ToStar(filename);
            collection.InsertMany(stars);
            Console.WriteLine("Tid att lagra {0} stjärnor till MongoDB-databas:                        {1}\n", starAmount, oldTime.Elapsed);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="starAmount"></param>
        public static void ReadMongoDB(IMongoDatabase database)
        {
            Char choice = '0';
            int collectionIndex;
            int starIndex;

            do
            {
                Console.WriteLine("\nVälj en av följande tillgängliga tabeller (a avbryter): ");
                using (var cursor = database.ListCollections())
                {
                    int i = 0;
                    foreach (var document in cursor.ToEnumerable())
                    {
                        Console.WriteLine("{0}: stars{1}", i + 1, Math.Pow(10, i + 1));
                        i += 1;
                    }
                }
                choice = Console.ReadKey().KeyChar;

                if (choice == 'a') ;
                else
                {

                    try
                    {
                        collectionIndex = Convert.ToInt32(choice) - 48;
                        Console.WriteLine("\n\nVänligen ange indexet på stjärnan du skulle vilja förhandsgranska: ");
                        starIndex = Convert.ToInt32(Console.ReadLine());
                        Stopwatch oldTime = new Stopwatch();
                        oldTime.Start();
                        var filter = Builders<Star>.Filter.Eq("_id", starIndex);
                        var collection = database.GetCollection<Star>(String.Format("stars{0}", Math.Pow(10, collectionIndex)));
                        var document = collection.Find(filter).First();
                        Console.WriteLine("ID | PosX | PosY | Radius | Colour | Activity | Age");
                        Console.WriteLine(document);
                        oldTime.Stop();
                        Console.WriteLine("Total tid att söka upp 1 objekt:                             {0}\n", oldTime.Elapsed);
                    }

                    catch (Exception e)
                    {
                        if (choice == 'a')
                            Console.WriteLine("Du har valt att inte förhandsgranska några element.");
                        else
                            Console.WriteLine("Du angav ett felaktigt alternativ, vänligen försök igen.");
                    }
                }

            }
            while (choice != 'a');
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="starAmount"></param>
        /// <param name="percentage"></param>
        /// <param name="repeats"></param>
        /// <param name="collection"></param>
        public static void UpdateMongoDB(int starAmount, int percentage, int repeats, IMongoCollection<Star> collection)
        {
            Random rnd = new Random();
            List<int> totalChanged = new List<int>();
            List<TimeSpan> timeAverage = new List<TimeSpan>();
            Stopwatch oldTime = new Stopwatch();
            Stopwatch oldTimeTotal = new Stopwatch();
            oldTimeTotal.Start();

            int changedAmount = 0;
            StreamWriter sw = new StreamWriter(string.Format("MongoDB_Data{0}_{1}%.txt", starAmount, percentage));
            sw.Close();

            for (int i = 0; i < repeats; i++)
            {
                // Resettar alla värden
                changedAmount = 0;
                oldTime.Reset();
                oldTime.Start();

                for (int u = 0; u < starAmount; u++)
                {
                    int randInt = rnd.Next();

                    if (randInt % 100 < percentage)
                    {
                        changedAmount += 1;

                        Star s = RandomStar(rnd, u + 1);

                        var filter = Builders<Star>.Filter.Eq("id", s._id);
                        var update = Builders<Star>.Update.Set("posX", s.PosX).Set("posY", s.PosY).Set("radius", s.Radius).Set("age", s.Age).Set("activity", s.Activity).Set("colour", s.Colour);

                        collection.UpdateOne(filter, update);
                    }
                }
                // Stoppar klockan här så att öppningen av ett nytt StreamWriter-objekt inte påverkar tiden
                oldTime.Stop();

                sw = new StreamWriter(string.Format("MongoDB_Data{0}_{1}%.txt", starAmount, percentage), true);
                sw.WriteLine("{0}|{1}", changedAmount, oldTime.Elapsed);
                sw.Close();
                totalChanged.Add(changedAmount);
                timeAverage.Add(oldTime.Elapsed);
            }
            oldTimeTotal.Stop();
            int changed = 0;
            foreach (int i in totalChanged)
                changed += i;

            TimeSpan time = new TimeSpan();
            foreach (TimeSpan t in timeAverage)
                time += t;

            Console.WriteLine("Genomsnittlig tid att ändra {0} av {1} objekt i MongoDB:                {2}", changed / repeats, starAmount, new TimeSpan(time.Ticks / repeats));
            Console.WriteLine("Total tid att ändra i genomsnitt {0} objekt {1} gånger:          {2}\n", changed / repeats, repeats, oldTimeTotal.Elapsed);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="starAmount"></param>
        /// <param name="collection"></param>
        public static void DeleteMongoDB(int starAmount, IMongoCollection<Star> collection)
        {
            Random rnd = new Random();
            int id = rnd.Next(1, starAmount + 1);
            Stopwatch oldTime = new Stopwatch();
            oldTime.Start();
            Console.WriteLine("Tar bort stjärnan med id: {0}", id);

            var filter = Builders<Star>.Filter.Eq("_id", id);
            collection.DeleteOne(filter);
            Console.WriteLine("Tid att ta bort 1 objekt från MongoDB-databasen: {0}", oldTime.Elapsed);
        }
    }
}
