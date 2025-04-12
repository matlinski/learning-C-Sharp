// See https://aka.ms/new-console-template for more information
using System;
using System.IO;
using System.Globalization;
using Microsoft.Data.Sqlite;
using static BCrypt.Net.BCrypt;

public class Session{
    public int userID;
    public string username;
    public string hash;
    public DateTime loggedInAt;
    public Session(
        int id,
        string name,
        string cachedHash = "none"
    ){
        userID = id;
        username = name;
        DateTime loggedInAt = DateTime.Now;
        if(cachedHash == "none"){
            hash = HashPassword(
                userID+"__"+username+"__"+loggedInAt+Guid.NewGuid()
            ).Replace('/', '\\');
            return;
        }
        hash = cachedHash;
    }
}

public class App{
    private string sessionDirectory = "/tmp/session";
    private SqliteConnection connection;
    private int giveUserNumberedOptions (int[] validOptions) {
        do
        {
            string userInput = Console.ReadLine().ToString();
            if(userInput == "exit"){
                break;
            }
            if(!(
                Int32.TryParse(userInput, out int action)
                && validOptions.Contains(action)
            )){
                Console.WriteLine("This is not a valid response, only the following are valid:");
                foreach (int validOption in validOptions){
                    Console.WriteLine(String.Concat("- ", validOption));
                }
                return 0;
            }
            return action;
        } while (true);
        return 0;
    }
    private string validateUsername( int attempt = 0){
        if(attempt > 3){
            Console.WriteLine("Too many failed attempts");
            System.Environment.Exit(1);
        }
        Console.WriteLine("Set your username");
        string username = Console.ReadLine().ToString();
        var command = connection.CreateCommand();
        command.CommandText = 
        @"
            SELECT id
            FROM user
            WHERE username = $username;
        ";
        command.Parameters.AddWithValue("$username", username);
        var reader = command.ExecuteReader();
        if(reader.Read()){
            Console.WriteLine("Username already exists, choose another one.");
            attempt = attempt + 1;
            return validateUsername(attempt);
        }
        return username;
    }
    private string maskUserInput(){
        var password = string.Empty;
        ConsoleKey key;
        do {
            var keyInfo = Console.ReadKey(intercept: true);
            key = keyInfo.Key;

            if (key == ConsoleKey.Backspace && password.Length > 0){
                Console.Write("\b \b");
                password = password[0..^1];
            }
            else if(!char.IsControl(keyInfo.KeyChar)) {
                Console.Write("*");
                password += keyInfo.KeyChar;
            }
        } while (key != ConsoleKey.Enter);
        Console.Write("\n");
        return password;
    }
    private string validatePassword(int attempt = 0){
        if(attempt > 3){
            Console.WriteLine("Too many failed attempts");
            System.Environment.Exit(1);
        }
        Console.WriteLine("Set your password");
        string password = maskUserInput();
        Console.WriteLine("Repeat the password");
        
        string passwordConfirmation = maskUserInput();
        if(password != passwordConfirmation){
            Console.WriteLine("Passwords don't match");
            attempt = attempt + 1;
            return validatePassword(attempt);
        }
        return password;
    }
    private int validateAccountCredentials(string username, int attempt = 0) {
        if(attempt > 3){
            Console.WriteLine("Too many failed attempts");
            return 0;
        }
        Console.WriteLine("Enter password:");
        string password = maskUserInput();
        var command = connection.CreateCommand();
        command.CommandText =
            @"
            SELECT password, id
            FROM user
            WHERE username IN (
                'dummy',
                $username
            )
            ORDER BY id DESC;
            "
        ;
        command.Parameters.AddWithValue(
            "$username",
            username
        );
        var reader = command.ExecuteReader();
        reader.Read();
        string passwordHashEntry = reader.GetString(0);
        string passwordHash = HashPassword($"{password}-dummy");
        if(passwordHashEntry != "dummy"){
            passwordHash = passwordHashEntry;
        }
        if(Verify(password, passwordHash)){
            return Int32.Parse(reader.GetString(1));
        }
        Console.WriteLine("User and password don't match any account.");
        attempt = attempt + 1;
        return validateAccountCredentials(username, attempt);
    }

    public App(){
        if (!Directory.Exists(sessionDirectory))
        {
            Directory.CreateDirectory(sessionDirectory);
        }
        connection = new SqliteConnection("Data Source=app.db");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = 
        @"
            CREATE TABLE IF NOT EXISTS `user` (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL,
                password TEXT NOT NULL,
                created_at TEXT DEFAULT (current_timestamp),
                modified_at TEXT DEFAULT (current_timestamp),
                UNIQUE(username)
            );
            CREATE TABLE IF NOT EXISTS `session` (
                id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                hash TEXT NOT NULL,
                created_at TEXT DEFAULT (current_timestamp) NOT NULL,
                UNIQUE(hash),
                CONSTRAINT session_user_FK FOREIGN KEY (user_id)
                REFERENCES `user`(id) ON DELETE CASCADE ON UPDATE CASCADE
            );
        ";
        command.ExecuteReader();
        command = connection.CreateCommand();
        command.CommandText = 
            @"
            INSERT INTO `user` (username, password)
            VALUES ('dummy', 'dummy')
            ON CONFLICT(username) DO NOTHING;
            "
        ;
        command.ExecuteReader();
        DirectoryInfo sessionDirInfo = new DirectoryInfo(sessionDirectory);
        string sessionHash = sessionDirInfo.GetFiles().Select(fi=>fi.Name).FirstOrDefault();
        command = connection.CreateCommand();
        command.CommandText =
            @"
            SELECT user_id FROM `session`
            WHERE `hash` = $hash
            "
        ;
        command.Parameters.AddWithValue("$hash", sessionHash);
        var reader = command.ExecuteReader();
        reader.Read();
        int userID = Int32.Parse(reader.GetString(0)); //TODO: if no result set 0
        command = connection.CreateCommand();
        command.CommandText = 
            @"
            SELECT username FROM user WHERE id = $id
            "
        ;
        command.Parameters.AddWithValue("$id", userID);
        reader = command.ExecuteReader();
        reader.Read();
        string username = reader.GetString(0); //TODO: if no result set an empty string
        if(userID == 0) {
            Console.WriteLine("Hello,what would you like to do?");
            Console.WriteLine("1) login");
            Console.WriteLine("2) register");
            Console.WriteLine("Pass exit to terminate");
            int[] validOptions = {1, 2};
            int choice = giveUserNumberedOptions(
                validOptions
            );
            if(choice == 1){
                Console.WriteLine("Enter username:");
                username = Console.ReadLine().ToString();
                userID =  validateAccountCredentials(username);
            }
            else if(choice == 2){
                username = validateUsername();
                string validatedPassword = validatePassword();
                string passwordHash = HashPassword(validatedPassword);
                command = connection.CreateCommand();
                command.CommandText = 
                    @"
                    INSERT INTO `user` (username, password)
                    VALUES ($username, $password)
                    "
                ;
                command.Parameters.AddWithValue(
                    "$username",
                    username
                );
                command.Parameters.AddWithValue(
                    "$password",
                    passwordHash
                );
                command.ExecuteReader();
                command = connection.CreateCommand();
                command.CommandText = "SELECT id FROM user WHERE username = $username";
                command.Parameters.AddWithValue(
                    "$username",
                    username
                );
                reader = command.ExecuteReader();
                reader.Read();
                userID = Int32.Parse(reader.GetString(0));
            }
            if(userID == 0){
                return;
            }
        }
        Session session = new Session(
            userID,
            username
        );
        command = connection.CreateCommand();
        command.CommandText =
            @"
            INSERT INTO session (user_id, hash)
            VALUES ($user_id, $hash)
            "
        ;
        command.Parameters.AddWithValue(
            "$user_id",
            session.userID
        );
        command.Parameters.AddWithValue(
            "hash",
            session.hash
        );
        command.ExecuteReader();
        StreamWriter sessionFile = new StreamWriter(
            sessionDirectory+'/'+session.hash
        );
        // session.hash
        Console.WriteLine($"welcome back {session.username}");
    }
}

