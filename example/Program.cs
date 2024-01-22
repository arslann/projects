
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Data.SQLite;

namespace HelloWorld
{
    class Program
    {


        static async Task Main(string[] args)
        {



            string accessToken = (await getAcessToken()).ToString();

            var products = await FetchProducts(accessToken);

            SQLiteConnection connection = ConnectDB();
            string createTableSql = "CREATE TABLE IF NOT EXISTS products (id INTEGER PRIMARY KEY, name TEXT, productCode TEXT, quantity LONG)";
            SQLiteCommand createTableCommand = new SQLiteCommand(createTableSql, connection);


            try
            {
                connection.Open();
                createTableCommand.ExecuteNonQuery();


                // DB boş ise ürünleri ekle
                var countCommand = getDatabaseLength(connection);

                if (countCommand)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Product product = new Product
                        {
                            name = products.GetProperty("productList")[i].GetProperty("name").ToString(),
                            productCode = products.GetProperty("productList")[i].GetProperty("productCode").ToString(),
                            quantity = int.Parse(products.GetProperty("productList")[i].GetProperty("quantity").ToString())
                        };

                        // Ürünü database e ekle
                        var insertCommand = InsertProductRecord(connection, product);
                        insertCommand.ExecuteNonQuery();
                    }
                }

                Console.WriteLine("Connected to SQLite!");

                while (true)
                {
                    Console.WriteLine("Lütfen seçim yapınız.");
                    Console.WriteLine("---------------------");
                    Console.WriteLine("1 - Ürünleri listele");
                    Console.WriteLine("2 - Stok Güncelle");
                    Console.WriteLine("3 - Ürün sil");
                    Console.WriteLine("4 - Çıkış yap");
                    string userInput = Console.ReadLine();


                    switch (userInput)
                    {
                        case "1":
                            ListProducts(connection);
                            break;
                        case "2":
                            UpdateProduct(connection);
                            break;
                        case "3":
                            RemoveProduct(connection);
                            break;
                        case "4":
                            return;
                        default:

                            break;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                connection.Close();
            }
        }

        private static void RemoveProduct(SQLiteConnection connection)
        {
            Console.WriteLine("Ürünü silmek istediğiniz stok kodunu girin:");
            string productCode = Console.ReadLine();

            string deleteSql = "DELETE FROM products WHERE productCode = @productCode";
            SQLiteCommand deleteCommand = new SQLiteCommand(deleteSql, connection);

            deleteCommand.Parameters.AddWithValue("@productCode", productCode);

            int rowsAffected = deleteCommand.ExecuteNonQuery();

            if (rowsAffected > 0)
            {
                Console.WriteLine("Ürün başarıyla silindi.\n");
            }
            else
            {
                Console.WriteLine("Ürün bulunamadı.\n");
            }
        }

        private static void ListProducts(SQLiteConnection connection)
        {
            var selectCommand = new SQLiteCommand("SELECT * FROM products", connection);
            var reader = selectCommand.ExecuteReader();

            while (reader.Read())
            {
                Console.WriteLine($"Name: {reader["name"]}, Product Code: {reader["productCode"]}, Quantity: {reader["quantity"]}");
            }
            System.Console.WriteLine("\n");
            reader.Close();
        }

        private static void UpdateProduct(SQLiteConnection connection)
        {
            Console.WriteLine("Güncellemek istediğiniz ürünün stok kodunu girin:");
            string productCode = Console.ReadLine();

            Console.WriteLine("Yeni stok miktarı:");
            long quantity = long.Parse(Console.ReadLine());

            string updateSql = "UPDATE products SET quantity = @quantity WHERE productCode = @productCode";
            SQLiteCommand updateCommand = new SQLiteCommand(updateSql, connection);

            // Parameters
            updateCommand.Parameters.AddWithValue("@quantity", quantity);
            updateCommand.Parameters.AddWithValue("@productCode", productCode);

            int rowsAffected = updateCommand.ExecuteNonQuery();

            if (rowsAffected > 0)
            {
                Console.WriteLine("ürün başarıyla güncellendi.\n");
            }
            else
            {
                Console.WriteLine("Ürün bulunamadı.\n");
            }
        }

        private static async Task<JsonElement> FetchProducts(string accessToken)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://apiv2.entegrabilisim.com/product/page=1/");
            request.Headers.Add("Authorization", "JWT " + accessToken);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var res = await response.Content.ReadFromJsonAsync<JsonElement>();

            return res;
        }

        private static SQLiteCommand InsertProductRecord(SQLiteConnection connection, Product product)
        {
            string insertSql = "INSERT INTO products (name, productCode, quantity) VALUES (@name, @productCode, @quantity)";

            SQLiteCommand insertCommand = new SQLiteCommand(insertSql, connection);

            // Parameters
            insertCommand.Parameters.AddWithValue("@name", product.name);
            insertCommand.Parameters.AddWithValue("@productCode", product.productCode);
            insertCommand.Parameters.AddWithValue("@quantity", product.quantity);

            return insertCommand;
        }
        private static bool getDatabaseLength(SQLiteConnection connection)
        {
            string countSql = "SELECT COUNT(*) FROM products";
            SQLiteCommand countCommand = new SQLiteCommand(countSql, connection);


            int count = Convert.ToInt32(countCommand.ExecuteScalar());

            return count == 0;
        }

        private static SQLiteConnection ConnectDB()
        {
            string connectionString = "Data Source=mydatabase.db;Version=3;";
            SQLiteConnection connection = new SQLiteConnection(connectionString);


            return connection;

        }

        private static async Task<JsonElement> getAcessToken()
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://apiv2.entegrabilisim.com/api/user/token/obtain/");
            var content = new StringContent(" {\r\n     \"email\": \"apitestv2@entegrabilisim.com\",\r\n     \"password\":\"apitestv2\"\r\n }", null, "application/json");
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var res = await response.Content.ReadFromJsonAsync<JsonElement>();

            var accessToken = res.GetProperty("access");

            Console.WriteLine("Access Token: " + accessToken);

            return accessToken;
        }


        // create async method


    }
}

class Product
{
    public string? name { get; set; }
    public string? productCode { get; set; }
    public long quantity { get; set; }
}