// See https://aka.ms/new-console-template for more information
using Microsoft.Data.Sqlite;
using static BCrypt.Net.BCrypt;

var app = new App();

public class App{
    private SqliteConnection connection;
    private int giveUserNumberedOptionsAndReturnTheChoice (int[] validOptions) {
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
    private bool validateAccountCredentials(string username, int attempt = 0) {
        if(attempt > 3){
            Console.WriteLine("Too many failed attempts");
            return false;
        }
        Console.WriteLine("Enter password:");
        string password = maskUserInput();
        var command = connection.CreateCommand();
        command.CommandText =
            @"
            SELECT password
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
            return true;
        }
        Console.WriteLine("User and password don't match any account.");
        attempt = attempt + 1;
        return validateAccountCredentials(username, attempt);
    }

    public App(){
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
        ";
        command.ExecuteReader();
        command = connection.CreateCommand();
        command.CommandText = 
            @"
            INSERT INTO `user` (username, password)
            VALUES ('dummy', 'dummy')
            "
        ;
        command.ExecuteReader();
        Console.WriteLine("Hello,what would you like to do?");
        Console.WriteLine("1) login");
        Console.WriteLine("2) register");
        Console.WriteLine("Pass exit to terminate");
        int[] validOptions = {1, 2};
        int choice = giveUserNumberedOptionsAndReturnTheChoice(
            validOptions
        );
        if(choice == 1){
            Console.WriteLine("Enter username:");
            string username = Console.ReadLine().ToString();
            bool passwordValid = validateAccountCredentials(username);
            if(passwordValid){
                Console.WriteLine($"welcome back {username}");
            }
        }
        else if(choice == 2){
            string validatedUsername = validateUsername();
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
                validatedUsername
            );
            command.Parameters.AddWithValue(
                "$password",
                passwordHash
            );
            command.ExecuteReader();        }
    }
}

