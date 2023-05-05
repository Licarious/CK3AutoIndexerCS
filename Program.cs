
using CK3AutoIndexerCS;
using System;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Text;

internal class Program
{
    private static void Main(string[] args) {
        //check if the user has the correct version of .NET installed
        if (Environment.Version.Major < 6) {
            Console.WriteLine("You need to have .NET 6.0 or higher installed to run this program.");
            Console.ReadKey();
            return;
        }

        //config
        int startingID = 9283;
        int endingID = 9773;
        int newStartingID = 9296;
        //diffrence between the two starting IDs
        int idDiffrence = newStartingID - startingID;

        string game = "IR"; //IR or CK3


        //localDir move 3 directories up
        string localDir = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).ToString()).ToString()).ToString();

        List<Province> provList = parseDefinition();
        //General functions
        updateDefaultMap();
        updateDeffinitions(provList);
        updateMapObjects();
        updateAdjacencies();
        updateLocal();
        
        
        //IR functions
        updateSetupMain();
        updateSetupProvinces();
        updateAreas();
        updatePorts();

        //CK3 functions
        updateLandedTitles();
        updateProvinceTerrain();

        DrawUpdateMap(provList);



        //parse the definition.csv
        List<Province> parseDefinition() {
            List<Province> provinces = new();
            string[] lines = File.ReadAllLines(localDir + @"\_Input\map_data\definition.csv");
            foreach (string line in lines) {
                if (line.Trim() == "") continue;
                string[] split = line.Split(';');
                if (line.StartsWith("#")) {
                    //remove leading # and trim
                    string l1 = line[1..].Trim();
                    //if first char is not a number, it's a comment
                    if (!char.IsDigit(l1[0])) continue;
                    split = l1.Split(';');
                }

                //if first split is 0 continue
                if (split[0] == "0") continue;

                Province p = new(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]), int.Parse(split[3]), split[4]);
                provinces.Add(p);

                if (split.Length > 6) {
                    if (split[6].Trim() != "") {
                        p.otherInfo = split[6];
                    }
                }

            }

            //if the last province id is less than the new starting ID + (endingID - startingID),
            //geneate new provicnes with random colors not used by the original provinces
            if (provinces[^1].id < newStartingID + (endingID - startingID)) {
                int newID = provinces[^1].id + 1;
                //round up needed id to the nearest 1000
                int endID = (int)Math.Ceiling((newStartingID + (endingID - startingID)) / 1000.0) * 1000;
                //get all the colors used by the original provinces
                List<Color> usedColors = new();
                foreach (Province p in provinces) {
                    usedColors.Add(p.color);
                }
                //generate new provinces with random colors not used by the original provinces
                while (newID < endID) {
                    Color c = Color.FromArgb(255, new Random().Next(0, 255), new Random().Next(0, 255), new Random().Next(0, 255));
                    if (!usedColors.Contains(c)) {
                        provinces.Add(new Province(newID, c.R, c.G, c.B, ""));
                        newID++;
                    }
                }
            }


            //move name and other info from old prov ids (startingID to endingID incluseive) to new prov ids (newStartingID to ... inclusive), in reverse order
            for (int i = endingID; i >= startingID; i--) {
                Province oldProv = provinces.Find(x => x.id == i);
                if (oldProv == null) continue;
                Province newProv = provinces.Find(x => x.id == i + idDiffrence);
                if (newProv == null) continue;
                newProv.name = oldProv.name;
                newProv.otherInfo = oldProv.otherInfo;
                //set oldProv.newProv to newProv
                oldProv.newProv = newProv;
                //clear oldProv.name and oldProv.otherInfo
                oldProv.name = "";
                oldProv.otherInfo = "";

            }
            
            return provinces;

        }


        //update the default.map file
        void updateDefaultMap() {
            //if the file doesn't exist, exit
            if (!File.Exists(localDir + @"\_Input\map_data\default.map")) {
                Console.WriteLine("default.map not found");
                return;
            }

            //check if output folder exists
            if (!Directory.Exists(localDir + @"\_Output\map_data\")) {
                Directory.CreateDirectory(localDir + @"\_Output\map_data\");
            }

            string[] lines = File.ReadAllLines(localDir + @"\_Input\map_data\default.map");
            //open output file
            using StreamWriter sw = new(localDir + @"\_Output\map_data\default.map");
            foreach (string line in lines) {
                string l1 = CleanLine(line);
                if (l1 == "") {
                    //write comment line if any and continue
                    sw.WriteLine(line);
                    continue;
                }
                string[] worlds = l1.Split();

                //check each world for the old IDs and replace them with the new ones
                for (int i = 0; i < worlds.Length; i++) {
                    //if the world is a number between startingID and endingID inclusive
                    if (int.TryParse(worlds[i], out int id)) {
                        if (id >= startingID && id <= endingID) {
                            //replace the old ID with the new one
                            worlds[i] = (id + idDiffrence).ToString();
                        }
                    }
                }

                //write the new line to the output file
                sw.Write(string.Join(" ", worlds));
                //if the line has a comment, write it to the output file
                if (line.Contains('#')) {
                    sw.Write("\t" + line[line.IndexOf("#")..]);
                }
                sw.WriteLine();


            }
            sw.Close();
        }
        
        //write updated definition.csv
        void updateDeffinitions(List<Province> provList) {
            //if definition.csv does not exists, exit
            if (!File.Exists(localDir + @"\_Input\map_data\definition.csv")) {
                Console.WriteLine("definition.csv not found");
                return;
            }

            //check if output folder exists
            if (!Directory.Exists(localDir + @"\_Output\map_data\")) {
                Directory.CreateDirectory(localDir + @"\_Output\map_data\");
            }

            string IRdeffEnd = "x;;;;;;;;;;;;;;;;;;;,\n";

            //open output file
            using StreamWriter sw = new(localDir + @"\_Output\map_data\definition.csv");
            sw.WriteLine("#Province id 0 is ignored, hard coded.\n0;0;0;0;x;x;");
            foreach (Province p in provList) {
                //add a leading # to the line if the province id is greater than newStartingID + (endingID - startingID)
                if (p.id > newStartingID + (endingID - startingID)) {
                    sw.Write("#");
                }
                sw.Write($"{p.id};{p.color.R};{p.color.G};{p.color.B};{p.name};");
                if (game == "IR") {
                    sw.Write(IRdeffEnd);
                }
                else {
                    if (p.otherInfo != "") {
                        sw.Write(p.otherInfo + ";");
                    }
                    sw.Write("x;\n");
                }
            }

            sw.Close();
        }

        //write updated map_object_data files
        void updateMapObjects() {
            //if gfx/map/map_object_data folder does not exist, exit
            if (!Directory.Exists(localDir + @"\_Input\gfx\map\map_object_data\")) {
                Console.WriteLine("gfx/map/map_object_data folder not found");
                return;
            }

            //check if output folder exists
            if (!Directory.Exists(localDir + @"\_Output\gfx\map\map_object_data")) {
                Directory.CreateDirectory(localDir + @"\_Output\gfx\map\map_object_data");
            }
            
            //open each file in the map_object_data folder
            foreach (string file in Directory.GetFiles(localDir + @"\_Input\gfx\map\map_object_data")) {
                string[] lines = File.ReadAllLines(file, Encoding.Unicode);
                //open output file
                using StreamWriter sw = new(localDir + @"\_Output\gfx\map\map_object_data\" + Path.GetFileName(file));
                foreach (string line in lines) {
                    //update the province IDs
                    if (line.Trim().StartsWith("id") && line.Contains('=')) {
                        //split on = and check if the second part is an int (province ID)
                        //if it is an int check if is in the range of old IDs
                        //if it is in the range of old IDs, replace it with the new ID
                        string[] split = line.Split('=');
                        if (int.TryParse(split[1], out int id)) {
                            if (id >= startingID && id <= endingID) {
                                split[1] = (id + idDiffrence).ToString();
                                
                            }
                        }
                        sw.WriteLine(string.Join("=", split));
                    }
                    //else just write the line to the output file
                    else {
                        sw.WriteLine(line);
                    }
                }
                sw.Close();

            }
        }

        //write updated adjacencies.csv
        void updateAdjacencies() {
            //if the adjacencies file does not exist, exit
            if (!File.Exists(localDir + @"\_Input\map_data\adjacencies.csv")) {
                Console.WriteLine("adjacencies.csv not found");
                return;
            }

            //check if output folder exists
            if (!Directory.Exists(localDir + @"\_Output\map_data\")) {
                Directory.CreateDirectory(localDir + @"\_Output\map_data\");
            }

            //read the adjacencies.csv file
            string[] lines = File.ReadAllLines(localDir + @"\_Input\map_data\adjacencies.csv");
            //open output file
            using StreamWriter sw = new(localDir + @"\_Output\map_data\adjacencies.csv");
            foreach (string line in lines) {
                if (line.Trim().StartsWith("#") || line.Trim() == "") {
                    //write comment line if any and continue
                    sw.WriteLine(line);
                    continue;
                }

                string[] split = line.Split(';');
                //check 1st, 2nd, and 4th elements for old IDs and replace them with the new ones
                for (int i = 0; i < split.Length; i++) {
                    if (int.TryParse(split[i], out int id)) {
                        if (id >= startingID && id <= endingID) {
                            split[i] = (id + idDiffrence).ToString();
                            //Console.WriteLine(id + " " + split[i]);
                        }
                    }
                    //exit after the 4th element
                    if (i == 3) break;
                }

                //write the new line to the output file
                sw.WriteLine(string.Join(";", split));

            }
        }

        //draw map with new colors replacing old colors
        void DrawUpdateMap(List<Province> provList) {
            //if provinces.png does not exist, exit
            if (!File.Exists(localDir + @"\_Input\map_data\provinces.png")) {
                Console.WriteLine("provinces.png not found");
                return;
            }

            //check if output folder exists
            if (!Directory.Exists(localDir + @"\_Output\map_data\")) {
                Directory.CreateDirectory(localDir + @"\_Output\map_data\");
            }

            //open _Input\map_data\provinces.png
            Bitmap bmp = new(localDir + @"\_Input\map_data\provinces.png");

            //create a new bitmap with the same size as the input bitmap
            Bitmap newBmp = new(bmp.Width, bmp.Height);
            
            //dictonary for pairing old and new colors
            Dictionary<Color, Color> colorPairs = new();
            //go through each province and if their id is in the range startingID to endingID (inclusive) add the old and new color to the lists
            foreach (Province p in provList) {
                //if newProv is null, continue
                if (p.newProv == null) continue;
                if (p.id >= startingID && p.id <= endingID) {
                    colorPairs.Add(p.color, p.newProv.color);
                }
            }

            Console.WriteLine("Drawing new map...");
            //go through each pixel in the bmp if the color is in the oldColors list, replace it with the corresponding color in the newColors list
            for (int x = 0; x < bmp.Width; x++) {
                for (int y = 0; y < bmp.Height; y++) {
                    Color c = bmp.GetPixel(x, y);
                    if (colorPairs.ContainsKey(c)) {
                        newBmp.SetPixel(x, y, colorPairs[c]);
                    }
                }
            }

            //save the new bitmap to the output folder
            newBmp.Save(localDir + @"\_Output\map_data\provinces.png");

            //close the bitmaps
            bmp.Dispose();
            newBmp.Dispose();
        }

        //update setup/main files
        void updateSetupMain() {
            //if setup folder doesn't exist, exit
            if (!Directory.Exists(localDir + @"\_Input\setup\main\")) {
                Console.WriteLine("setup/main folder not found");
                return;
            }

            //check if output folder exists
            if (!Directory.Exists(localDir + @"\_Output\setup\main\")) {
                Directory.CreateDirectory(localDir + @"\_Output\setup\main\");
            }

            //open each file in the setup folder
            foreach (string file in Directory.GetFiles(localDir + @"\_Input\setup\main\")) {
                string[] lines = File.ReadAllLines(file, Encoding.Unicode);
                //open output file
                using StreamWriter sw = new(localDir + @"\_Output\setup\main\" + Path.GetFileName(file));

                int indentiaton = 0;
                bool provincesFound = false;
                bool roadNetworkFound = false;
                bool countryFound = false;
                bool ownControlCoreFound = false;
                bool tradeFound = false;

                foreach (string line in lines) {
                    bool wroteLine = false;

                    if (indentiaton == 0) {
                        //provinces
                        if (line.Trim().StartsWith("provinces")) {
                            provincesFound = true;
                        }
                        //road_network
                        else if (line.Trim().StartsWith("road_network")) {
                            roadNetworkFound = true;
                        }
                        //countries
                        else if (line.Trim().StartsWith("country")) {
                            countryFound = true;
                        }
                        //trade
                        else if (line.Trim().StartsWith("trade")) {
                            tradeFound = true;
                        }

                    }
                    else if (indentiaton == 1) {
                        if (provincesFound) {
                            //check if the line contains a province ID in the range of old IDs
                            if (int.TryParse(line.Split('=')[0].Trim(), out int id)) {
                                if (id >= startingID && id <= endingID) {
                                    //if it is in the range of old IDs, replace it with the new ID
                                    sw.WriteLine(line.Replace(id.ToString(), (id + idDiffrence).ToString()));
                                    wroteLine = true;
                                }
                            }
                        }
                        else if (roadNetworkFound) {
                            //split line on space
                            string[] split = line.Split();
                            //check each element in split for an int in the range of old IDs
                            string l1 = line;
                            for (int i = 0; i < split.Length; i++) {
                                if (int.TryParse(split[i], out int id)) {
                                    if (id >= startingID && id <= endingID) {
                                        //if it is in the range of old IDs, replace it with the new ID
                                        split[i] = (id + idDiffrence).ToString();
                                        l1 = l1.Replace(id.ToString(), (id + idDiffrence).ToString());
                                    }
                                }
                            }
                            //write the new line to the output file
                            sw.WriteLine(l1);
                            wroteLine = true;
                        }

                    }
                    else if (indentiaton == 2) {
                        if (countryFound) {
                            if(line.Contains("capital")){
                                //split on =
                                string[] split = line.Split('=');
                                //check if the 2nd element is an int in the range of old IDs
                                if (int.TryParse(split[1].Trim(), out int id)) {
                                    if (id >= startingID && id <= endingID) {
                                        //if it is in the range of old IDs, replace it with the new ID
                                        sw.WriteLine(line.Replace(id.ToString(), (id + idDiffrence).ToString()));
                                        wroteLine = true;
                                    }
                                }
                            }
                        }
                    }
                    else if (indentiaton == 3) {
                        //own_control_core
                        if (line.Trim().StartsWith("own_control_core")) {
                            ownControlCoreFound = true;
                        }
                    }
                    if (ownControlCoreFound) {
                        //split on space and check each element for an int in the range of old IDs
                        string[] split = line.Split();
                        string l1 = line;
                        for (int i = 0; i < split.Length; i++) {
                            if (int.TryParse(split[i], out int id)) {
                                if (id >= startingID && id <= endingID) {
                                    //if it is in the range of old IDs, replace it with the new ID
                                    split[i] = (id + idDiffrence).ToString();
                                    l1 = l1.Replace(id.ToString()+" ", (id + idDiffrence).ToString()+" ");
                                }
                            }
                        }
                        //write the new line to the output file
                        sw.WriteLine(l1);
                        wroteLine = true;
                    }

                    if (tradeFound) {
                        
                        //split on space and check each element for an int in the range of old IDs
                        string[] split = CleanLine(line).Split();
                        string l1 = line;
                        for (int i = 0; i < split.Length; i++) {
                            if (int.TryParse(split[i], out int id)) {
                                if (id >= startingID && id <= endingID) {
                                    //if it is in the range of old IDs, replace it with the new ID
                                    split[i] = (id + idDiffrence).ToString();
                                    l1 = l1.Replace(id.ToString(), (id + idDiffrence).ToString());
                                }
                            }
                        }
                        //write the new line to the output file
                        sw.WriteLine(l1);
                        wroteLine = true;
                    }



                    if (!wroteLine) sw.WriteLine(line);

                    //update the indentation
                    if (line.Contains('{') || line.Contains('}')) {
                        string[] l1 = CleanLine(line).Split();
                        foreach (string s in l1) {
                            if (s.Contains('{')) {
                                indentiaton++;
                            }
                            else if (s.Contains('}')) {
                                indentiaton--;
                                if (indentiaton == 0) {
                                    provincesFound = false;
                                    roadNetworkFound = false;
                                    countryFound = false;
                                    tradeFound = false;
                                }
                                else if (indentiaton == 3) {
                                    ownControlCoreFound = false;
                                }
                            }
                        }

                    }
                }
                sw.Close();

            }
        }

        //update setup/provinces
        void updateSetupProvinces() {
            //if setup folder doesn't exist, exit
            if (!Directory.Exists(localDir + @"\_Input\setup\provinces\")) {
                Console.WriteLine("\tsetup/provinces folder not found");
                return;
            }

            //check if output folder exists
            if (!Directory.Exists(localDir + @"\_Output\setup\provinces\")) {
                Directory.CreateDirectory(localDir + @"\_Output\setup\provinces\");
            }

            //read the files
            string[] files = Directory.GetFiles(localDir + @"\_Input\setup\provinces\");
            foreach (string file in files) {
                Console.WriteLine("\t\tUpdating " + file);

                //read the file
                string[] lines = File.ReadAllLines(file, Encoding.Unicode);
                //create a new file
                StreamWriter sw = new(file.Replace("_Input", "_Output"), false, Encoding.Unicode);
                //loop through the lines
                foreach (string line in lines) {
                    bool wroteLine = false;

                    if (line.Contains('=')) {
                        //split on = and check 1st element for an int in the range of old IDs
                        string[] split = line.Split('=');
                        if (int.TryParse(split[0].Trim(), out int id)) {
                            if (id >= startingID && id <= endingID) {
                                //if it is in the range of old IDs, replace it with the new ID
                                sw.WriteLine(line.Replace(id.ToString(), (id + idDiffrence).ToString()));
                                wroteLine = true;
                            }
                        }
                    }
                    
                    //write the new line to the output file
                    if (!wroteLine) sw.WriteLine(line);
                }
                sw.Close();
            }

        }

        //update areas
        void updateAreas() {
            //if areas.txt does not exist, exit
            if (!File.Exists(localDir + @"\_Input\map_data\areas.txt")) {
                Console.WriteLine("areas.txt not found");
                return;
            }

            int indentaiton = 0;
            bool provFound = false;

            //read the file
            string[] lines = File.ReadAllLines(localDir + @"\_Input\map_data\areas.txt");
            //create a new file
            StreamWriter sw = new StreamWriter(localDir + @"\_Output\map_data\areas.txt");

            //loop through the lines
            foreach (string line in lines) {
                bool wroteLine = false;
                
                if (indentaiton == 1) {
                    if (line.Contains("provinces")) {
                        provFound = true;
                    }
                }
                if (provFound) {
                    //split on space and check each element for an int in the range of old IDs
                    string[] split = CleanLine(line).Split();
                    string l1 = line;
                    for (int i = 0; i < split.Length; i++) {
                        if (int.TryParse(split[i], out int id)) {
                            if (id >= startingID && id <= endingID) {
                                //if it is in the range of old IDs, replace it with the new ID
                                split[i] = (id + idDiffrence).ToString();
                                l1 = l1.Replace(" "+id.ToString()+" ", " "+(id + idDiffrence).ToString()+" ");
                            }
                        }
                    }
                    //write the new line to the output file
                    sw.WriteLine(l1);
                    wroteLine = true;
                }

                if (!wroteLine) sw.WriteLine(line);

                if (line.Contains('{') || line.Contains('}')) {
                    string[] l1 = CleanLine(line).Split();
                    foreach (string s in l1) {
                        if (s.Contains('{')) {
                            indentaiton++;
                        }
                        else if (s.Contains('}')) {
                            indentaiton--;
                            if (indentaiton == 1) {
                                provFound = false;
                            }
                        }
                    }
                }
            }
            sw.Close();

        }

        //update ports
        void updatePorts() {
            //if ports.csv does not exist, exit
            if (!File.Exists(localDir + @"\_Input\map_data\ports.csv")) {
                Console.WriteLine("ports.csv not found");
                return;
            }

            //open the file
            string[] lines = File.ReadAllLines(localDir + @"\_Input\map_data\ports.csv");
            //create a new file
            StreamWriter sw = new StreamWriter(localDir + @"\_Output\map_data\ports.csv");

            //loop through the lines
            foreach (string line in lines) {
                //split on , and ;
                string[] split = line.Split(',', ';');

                //if 1st or 2nd element is an int in the range of old IDs, replace it with the new ID
                if (int.TryParse(split[0], out int id)) {
                    if (id >= startingID && id <= endingID) {
                        split[0] = (id + idDiffrence).ToString();
                    }
                }
                if (int.TryParse(split[1], out int id2)) {
                    if (id2 >= startingID && id2 <= endingID) {
                        split[1] = (id2 + idDiffrence).ToString();
                    }
                }

                //write the new line to the output file
                sw.WriteLine(string.Join(",", split));

                
            }
            sw.Close();


        }

        //update localization
        void updateLocal() {
            //if input folder doesn't exist, exit
            if (!Directory.Exists(localDir + @"\_Input\localization\")) {
                Console.WriteLine("localization folder not found");
                return;
            }

            //open in localization folder and subfolders
            string[] files = Directory.GetFiles(localDir + @"\_Input\localization\", "*.*", SearchOption.AllDirectories);
            //print the files
            foreach (string file in files) {
                //split last file from file path and get the path
                string[] split = file.Split('\\');
                string fileName = split[split.Length - 1];
                string path = file.Replace(fileName, "");

                //if file path does not exist in output folder, create it (not the file)
                if (!Directory.Exists(localDir + @"\_Output\localization\" + path.Replace(localDir + @"\_Input\localization\", ""))) {
                    Directory.CreateDirectory(localDir + @"\_Output\localization\" + path.Replace(localDir + @"\_Input\localization\", ""));
                }

                //read the file
                string[] lines = File.ReadAllLines(file, Encoding.Unicode);
                //create a new file
                StreamWriter sw = new(file.Replace(@"_Input\", @"_Output\"));
                 

                //loop through the lines
                foreach (string line in lines) {
                    bool wroteLine = false;

                    //if : in line split on it
                    if (line.Contains(':')) {
                        //key part
                        string word = line.Split(':')[0];
                        string tmpLine = line;
                        //if PROV is in the word
                        if (word.Contains("PROV")) {
                            //check if an id in the range of old IDs is in the word
                            string w2 = word.Split('_')[0];
                            if (int.TryParse(w2.Replace("PROV", ""), out int id)) {
                                if (id >= startingID && id <= endingID) {
                                    tmpLine = tmpLine.Replace(id.ToString(), (id + idDiffrence).ToString());
                                    wroteLine = true;
                                }
                            }
                        }

                        //value part
                        string value = line.Split(':')[1];
                        //if PROV is in the value
                        if (value.Contains("$PROV")) {
                            //split on $ and create a list of all string containing PROV
                            List<string> provs = new();
                            foreach (string s in value.Split('$')) {
                                if (s.Contains("PROV")) {
                                    provs.Add(s);
                                }
                            }

                            //check if an id in the range of old IDs is in the value
                            foreach (string s in provs) {
                                if (int.TryParse(value.Replace("PROV", ""), out int id)) {
                                    if (id >= startingID && id <= endingID) {
                                        tmpLine = tmpLine.Replace("PROV" + id.ToString(), "PROV" + (id + idDiffrence).ToString());
                                        wroteLine = true;
                                    }
                                }
                            }
                        }

                        if (wroteLine) sw.WriteLine(tmpLine);
                    }


                    //write the new line to the output file
                    if (!wroteLine) sw.WriteLine(line);
                }
                sw.Close();

            }

        }

        //update landedTitles
        void updateLandedTitles() {
            //if input file does not exist, return
            if (!Directory.Exists(localDir + @"\_Input\common\landed_titles\")) {
                Console.WriteLine("landed_titles folder not found");
                return;
            }

            //read all txt files in common/landed_titles
            string[] files = Directory.GetFiles(localDir + @"\_Input\common\landed_titles\", "*.txt", SearchOption.AllDirectories);

            //loop through the files
            foreach (string file in files) {
                //split last file from file path and get the path
                string[] split = file.Split('\\');
                string fileName = split[^1];
                string path = file.Replace(fileName, "");

                //if file path does not exist in output folder, create it (not the file)
                if (!Directory.Exists(localDir + @"\_Output\common\landed_titles\" + path.Replace(localDir + @"\_Input\common\landed_titles\", ""))) {
                    Directory.CreateDirectory(localDir + @"\_Output\common\landed_titles\" + path.Replace(localDir + @"\_Input\common\landed_titles\", ""));
                }

                //read the file as unicode
                string[] lines = File.ReadAllLines(file, Encoding.Unicode);
                //create a new file
                StreamWriter sw = new(file.Replace(@"_Input\", @"_Output\"));

                //loop through the lines
                foreach (string line in lines) {
                    bool wroteLine = false;

                    //if line contains province
                    if (line.Trim().StartsWith("province")) {
                        //split on = and check if the 2nd element is an int in the range of old IDs
                        string[] split2 = line.Split('=');
                        if (int.TryParse(split2[1].Split("#")[0].Trim(), out int id)) {
                            if (id >= startingID && id <= endingID) {
                                //replace the old ID with the new ID
                                sw.WriteLine(line.Replace(id.ToString(), (id + idDiffrence).ToString()));
                                wroteLine = true;
                            }
                        }
                    }

                    if (!wroteLine) sw.WriteLine(line);

                }

                sw.Close();
            }
        }

        //update provinceTerrain
        void updateProvinceTerrain() {
            //if input folder does not exist, return
            if (!Directory.Exists(localDir + @"\_Input\common\province_terrain\")) {
                Console.WriteLine("province_terrain folder not found");
                return;
            }

            //read all txt files in common/province_terrain
            string[] files = Directory.GetFiles(localDir + @"\_Input\common\province_terrain\", "*.txt", SearchOption.AllDirectories);

            //loop through the files
            foreach (string file in files) {
                //split last file from file path and get the path
                string[] split = file.Split('\\');
                string fileName = split[split.Length - 1];
                string path = file.Replace(fileName, "");

                //if file path does not exist in output folder, create it (not the file)
                if (!Directory.Exists(localDir + @"\_Output\common\province_terrain\" + path.Replace(localDir + @"\_Input\common\province_terrain\", ""))) {
                    Directory.CreateDirectory(localDir + @"\_Output\common\province_terrain\" + path.Replace(localDir + @"\_Input\common\province_terrain\", ""));
                }

                //read the file
                string[] lines = File.ReadAllLines(file, Encoding.Unicode);
                //create a new file
                StreamWriter sw = new(file.Replace(@"_Input\", @"_Output\"));

                //loop through the lines
                foreach (string line in lines) {
                    bool wroteLine = false;
                    
                    //if line has a = in it
                    if (line.Contains("=")) {
                        //split on = and check if the 2nd element is an int in the range of old IDs
                        string[] split2 = line.Split('=');
                        if (int.TryParse(split2[0].Trim(), out int id)) {
                            if (id >= startingID && id <= endingID) {
                                //replace the old ID with the new ID
                                sw.WriteLine(line.Replace(id.ToString(), (id + idDiffrence).ToString()));
                                wroteLine = true;
                            }
                        }
                    }

                    if (!wroteLine) sw.WriteLine(line);

                }

                sw.Close();
            }
        }


        string CleanLine(string line) {
            return line.Replace("{", " { ").Replace("}", " } ").Replace("=", " = ").Replace("  ", " ").Split('#')[0].Trim();
        }
    }
}