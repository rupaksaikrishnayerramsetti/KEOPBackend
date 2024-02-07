using System.Text;
using System.Security.Cryptography;

namespace KEOPBackend.helpers.Passwords
{
    public class Passwords
    {
        public string GeneratePassword()
        {
            string characters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()_+";
            StringBuilder password = new StringBuilder();
            int length = 8;
            Random random = new Random();
            for (int i = 0; i < length; i++)
            {
                int randomIndex = random.Next(0, characters.Length);
                char c = characters[randomIndex];
                password.Append(c);
            }
            return password.ToString();
        }

        public string GenerateDigest(string password) 
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                // Convert the input string to a byte array and compute the hash
                byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
                // Convert the byte array to a hexadecimal string
                StringBuilder stringBuilder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    stringBuilder.Append(b.ToString("x2"));
                }
                return stringBuilder.ToString();
            }
        }
    }
}
