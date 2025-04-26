using System;
using System.Data;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace MySql_Sorgu
{
    public partial class MainForm : Form
    {
        private MySqlConnection connection;
        private string connectionString = "Server=localhost;Uid=root;Pwd=;";

        public MainForm()
        {
            InitializeComponent();
            UpdateStatus("Bağlantı bekleniyor...");
        }

        private void UpdateStatus(string message)
        {
            lblStatus.Text = message;
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var form = new ConnectionForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                connectionString = form.ConnectionString;
                try
                {
                    connection = new MySqlConnection(connectionString);
                    connection.Open();
                    UpdateStatus($"Bağlantı başarılı: {connection.Database ?? "Genel"}");
                    LoadDatabases();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Bağlantı hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Bağlantı hatası");
                }
            }
        }

        private void LoadDatabases()
        {
            treeDatabases.Nodes.Clear();

            try
            {
                var cmd = new MySqlCommand("SHOW DATABASES", connection);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string dbName = reader.GetString(0);
                    var dbNode = new TreeNode(dbName);

                    if (!dbName.Equals("information_schema") &&
                        !dbName.Equals("mysql") &&
                        !dbName.Equals("performance_schema") &&
                        !dbName.Equals("sys"))
                    {
                        treeDatabases.Nodes.Add(dbNode);
                    }
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veritabanları yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void treeDatabases_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Parent == null)
            {
                try
                {
                    connection.ChangeDatabase(e.Node.Text);
                    UpdateStatus($"Bağlantı: {e.Node.Text}");
                    LoadTables(e.Node);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Veritabanı değiştirilemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (e.Node.Parent != null && e.Node.Parent.Parent == null)
            {
                try
                {
                    string query = $"SELECT * FROM {e.Node.Text} LIMIT 100";
                    ExecuteQuery(query);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Tablo yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadTables(TreeNode dbNode)
        {
            dbNode.Nodes.Clear();

            try
            {
                var cmd = new MySqlCommand("SHOW TABLES", connection);
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string tableName = reader.GetString(0);
                    dbNode.Nodes.Add(tableName);
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tablolar yüklenirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            ExecuteCurrentQuery();
        }

        private void txtQuery_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && e.Control)
            {
                e.SuppressKeyPress = true;
                ExecuteCurrentQuery();
            }
        }

        private void ExecuteCurrentQuery()
        {
            string query = txtQuery.SelectedText.Length > 0 ?
                txtQuery.SelectedText :
                txtQuery.Text;

            if (string.IsNullOrWhiteSpace(query))
            {
                MessageBox.Show("Lütfen bir SQL sorgusu yazın", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ExecuteQuery(query);
        }

        private void ExecuteQuery(string query)
        {
            try
            {
                var cmd = new MySqlCommand(query, connection);

                if (query.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    var adapter = new MySqlDataAdapter(cmd);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    dataGridView1.DataSource = dt;
                    tabControl1.SelectedTab = tabResults;
                    UpdateStatus($"Sorgu başarılı. {dt.Rows.Count} satır döndü.");
                }
                else
                {
                    int affectedRows = cmd.ExecuteNonQuery();
                    MessageBox.Show($"{affectedRows} satır etkilendi", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateStatus($"Sorgu başarılı. {affectedRows} satır etkilendi.");
                    LoadDatabases();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sorgu hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Sorgu hatası");
            }
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                connection.Close();
                treeDatabases.Nodes.Clear();
                dataGridView1.DataSource = null;
                UpdateStatus("Bağlantı kesildi");
            }
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                LoadDatabases();
                UpdateStatus("Veritabanı listesi yenilendi");
            }
        }

        private void createDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dbName = Prompt.ShowDialog("Yeni veritabanı adı:", "Veritabanı Oluştur");
            if (!string.IsNullOrWhiteSpace(dbName))
            {
                try
                {
                    var cmd = new MySqlCommand($"CREATE DATABASE `{dbName}`", connection);
                    cmd.ExecuteNonQuery();
                    LoadDatabases();
                    UpdateStatus($"Veritabanı oluşturuldu: {dbName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Veritabanı oluşturulamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void createTableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeDatabases.SelectedNode == null || treeDatabases.SelectedNode.Parent != null)
            {
                MessageBox.Show("Lütfen önce bir veritabanı seçin", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var tableName = Prompt.ShowDialog("Yeni tablo adı:", "Tablo Oluştur");
            if (!string.IsNullOrWhiteSpace(tableName))
            {
                try
                {
                    connection.ChangeDatabase(treeDatabases.SelectedNode.Text);
                    var cmd = new MySqlCommand($"CREATE TABLE `{tableName}` (id INT AUTO_INCREMENT PRIMARY KEY)", connection);
                    cmd.ExecuteNonQuery();
                    LoadTables(treeDatabases.SelectedNode);
                    UpdateStatus($"Tablo oluşturuldu: {tableName}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Tablo oluşturulamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}