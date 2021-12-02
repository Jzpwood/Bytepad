using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notebook
{
    public enum OwnershipType
    {
        NotOwned,
        OwnerCreator,
        SharedCollection,
        SharedItem
    }
    public static class Auth
    {
        public static OwnershipType CheckOwnership(string session_id, int item_id)
        {
            int user_id;

            if (Auth.ValidateSession(session_id, out user_id, out _))
                return CheckOwnership(user_id, item_id);
            else
                return OwnershipType.NotOwned;
        }

        public static OwnershipType CheckOwnership(int user_id, int item_id)
        {
            OwnershipType result = OwnershipType.NotOwned;

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(Query.validateOwnership, connection);
                command.Parameters.AddWithValue("@user_id", user_id);
                command.Parameters.AddWithValue("@item_id", item_id);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        int otype = (int)reader["owned"];

                        switch (otype)
                        {
                            case 0: { result = OwnershipType.NotOwned; break; }
                            case 1: { result = OwnershipType.OwnerCreator; break; }
                            case 2: { result = OwnershipType.SharedCollection; break; }
                            case 3: { result = OwnershipType.SharedItem; break; }
                        }
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            return result;
        }

        public static string TryLogin(string user, string password_hash)
            //Check login details
            //If valid, generate and return new session ID
            //Failure returns null
        {
            string session_id = "";
            int user_id;

            string uname = user.ToLowerInvariant();

            if (LoginRequest(user, password_hash, out user_id)) {
                session_id = GenerateSession(uname, DateTime.Now.AddDays(7), user_id);
                Event.Log(ApplicationEvent.UserLogin_Success, $"username:{uname},password_hash:{password_hash}");
            }
            else {
                Event.Log(ApplicationEvent.UserLogin_Fail, $"username:{user},password_hash:{password_hash}");
            }

            return session_id;
        }

        public static void Logout(string session_id)
            //Invalidate session id
        {
            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(Query.invalidateSessionQuery, connection);
                command.Parameters.AddWithValue("@session_id", session_id);
                connection.Open();
                command.ExecuteNonQuery();
                Event.Log(ApplicationEvent.UserLogin_Logout, $"session_id:{session_id}");
            }
        }

        public static bool ValidateSession(string session_id, out int user_id, out string username)
            //Check session ID in database
            //If valid and in not expired, return corresponding user ID
            //Failure returns -1
        {
            bool result = false;
            user_id = 0;
            username = "";

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(Query.validateSessionQuery, connection);
                command.Parameters.AddWithValue("@session_id", session_id);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        result = true;
                        user_id = (int)reader["user_id"];
                        username = (string)reader["username"];
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            return result;
        }

        public static bool LoginRequest(string username, string password_hash, out int user_id)
        //Validate user details against user table
        //Return true if correct, otherwise false
        {
            bool result = false;
            user_id = -1;

            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(Query.loginRequestQuery, connection);
                command.Parameters.AddWithValue("@login_username", username);
                command.Parameters.AddWithValue("@password_hash", password_hash);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        if (((string)reader["username"]).ToLowerInvariant() == username && (string)reader["password_hash"] == password_hash)
                        {
                            result = true;
                            user_id = (int)reader["id"];
                        }
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            return result;
        }
        public static string GenerateSession(string username, DateTime expiry, int user_id)
        {
            string session_id = "";
            bool id_valid = false;

            //Check if ID has been used already
            while (!id_valid)
            {
                session_id = NewID();
                using (SqlConnection connection = new SqlConnection(Query.connectionString))
                {
                    SqlCommand command = new SqlCommand(Query.sessionRequestQuery, connection);
                    command.Parameters.AddWithValue("@session_id", session_id);
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    try
                    {
                        if (!reader.HasRows) id_valid = true;
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
            }

            //Update session table
            using (SqlConnection connection = new SqlConnection(Query.connectionString))
            {
                SqlCommand command = new SqlCommand(Query.newSessionQuery, connection);
                command.Parameters.AddWithValue("@session_id", session_id);
                command.Parameters.AddWithValue("@user", user_id);
                command.Parameters.AddWithValue("@expiry", expiry);
                connection.Open();
                command.ExecuteNonQuery();
            }

            return session_id;
        }

        public static string NewID()
        {
            //Create random ID consisting of 20 letters and numbers
            Random r = new Random();

            string new_id = "";

            for (int i = 0; i < 20; i++)
            {
                int n = r.Next(0, 35);

                new_id += (char)((n < 26) ? (n + 'a') : (n + '0' - 26));
            }

            return new_id;
        }

    }
}
