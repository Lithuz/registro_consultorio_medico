using System.Data;
using System.Data.SqlClient;

namespace MedicalRegister
{
    public partial class frmUsers : Form
    {
        public frmUsers() => InitializeComponent();

        private bool EditMode = false;
        private protected string GlobalConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Database=MedicalSystem;Trusted_Connection=True;";
        public int CurrentId { get; set; }


        private void frmUsers_Load(object sender, EventArgs e)
        {
            CheckIfDatabaseExist();
            CheckIfTablesExist();
            CheckIfDefaultDataExist();

            ShowUserList();
        }

        private void ShowUserList()
        {
            dgvUsers.DataSource = null;
            dgvUsers.Refresh();

            DataTable userTable = new DataTable();

            var command = "SELECT * FROM Users WHERE IsActive = 1 ORDER BY ID desc";
            using (SqlConnection connection = new SqlConnection(GlobalConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(command, connection))
                {
                    cmd.CommandType = CommandType.Text;
                    var reader = cmd.ExecuteReader();
                    userTable.Load(reader);
                }
            }

            dgvUsers.DataSource = userTable;
        }

        private void CheckIfDefaultDataExist()
        {
            var commandList = new List<string>();
            commandList.Add("IF NOT EXISTS (SELECT * FROM Roles WHERE Name = 'Administrator') BEGIN INSERT INTO Roles(Name) VALUES ('Administrator') END");
            commandList.Add("IF NOT EXISTS (SELECT * FROM Roles WHERE Name = 'Doctor') BEGIN INSERT INTO Roles(Name) VALUES ('Doctor') END");
            commandList.Add("IF NOT EXISTS (SELECT * FROM Roles WHERE Name = 'Nurse') BEGIN INSERT INTO Roles(Name) VALUES ('Nurse') END");
            commandList.Add("IF NOT EXISTS (SELECT * FROM Roles WHERE Name = 'Patient') BEGIN INSERT INTO Roles(Name) VALUES ('Patient') END");

            using (SqlConnection connection = new SqlConnection(GlobalConnectionString))
            {
                connection.Open();

                foreach (string command in commandList)
                {
                    using (SqlCommand cmd = new SqlCommand(command, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void CheckIfTablesExist()
        {
            var commandList = new List<string>();
            commandList.Add("IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL " +
                "create table Roles(" +
                "Id int not null identity(1,1)," +
                "Name varchar(50) not null," +
                "IsActive bit default 1)");

            commandList.Add("IF OBJECT_ID(N'dbo.Users', N'U') IS NULL " +
                "create table Users(" +
                "Id int not null identity(1,1)," +
                "Name varchar(50) not null," +
                "IsActive bit default 1," +
                "RolId int default 4," +
                "DateAdded datetime not null," +
                "BirthDate date not null," +
                "Email nvarchar(50) not null)");

            using (SqlConnection connection = new SqlConnection(GlobalConnectionString))
            {
                connection.Open();

                foreach (string command in commandList)
                {
                    using (SqlCommand cmd = new SqlCommand(command, connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void CheckIfDatabaseExist()
        {
            var command = @"If(db_id(N'MedicalSystem') IS NULL) CREATE DATABASE [MedicalSystem]";

            using (var connection = new SqlConnection(@"Data Source=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;"))
            {
                connection.Open();

                using (SqlCommand cmd = new SqlCommand(command, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void UpdateControls(bool IsEnabled)
        {
            if (IsEnabled)
            {
                txtFullName.Enabled = true;
                txtEmail.Enabled = true;
                dtpBirthDate.Enabled = true;
                cbxRoles.Enabled = true;
                cbxRoles.SelectedIndex = 0;


                btnSaveData.Enabled = true;
                btnCancel.Enabled = true;
                btnAddUser.Enabled = false;
                btnDelete.Enabled = false;
                btnUpdate.Enabled = false;
            }
            else
            {
                txtFullName.Clear();
                txtEmail.Clear();
                dtpBirthDate.Value = DateTime.Now;

                txtFullName.Enabled = false;
                txtEmail.Enabled = false;
                dtpBirthDate.Enabled = false;
                cbxRoles.Enabled = false;
                cbxRoles.SelectedIndex = 0;

                btnSaveData.Enabled = false;
                btnCancel.Enabled = false;
                btnAddUser.Enabled = true;
                btnDelete.Enabled = true;
                btnUpdate.Enabled = true;
            }
        }
        private void btnAddUser_Click(object sender, EventArgs e)
        {
            UpdateControls(true);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            UpdateControls(false);
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {

            Int32 selectedRowCount =
                dgvUsers.Rows.GetRowCount(DataGridViewElementStates.Selected);
            if (selectedRowCount > 0)
            {
                EditMode = true;
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                DataTable userData = getUserDataFromDB((int)dgvUsers.SelectedRows[0].Cells["Id"].Value);

                if (userData != null)
                {
                    CurrentId = (int)userData.Rows[0]["Id"];
                    txtFullName.Text = userData.Rows[0]["Name"].ToString();
                    txtEmail.Text = userData.Rows[0]["Email"].ToString();
                    dtpBirthDate.Value = Convert.ToDateTime(userData.Rows[0]["BirthDate"]);

                    UpdateControls(true);
                    cbxRoles.SelectedIndex = (int)userData.Rows[0]["RolId"];
                }
            }
        }

        private DataTable getUserDataFromDB(int Id)
        {
            DataTable userTable = new DataTable();

            var command = $"SELECT * FROM Users WHERE Id = {Id}";
            using (SqlConnection connection = new SqlConnection(GlobalConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(command, connection))
                {
                    cmd.CommandType = CommandType.Text;
                    var reader = cmd.ExecuteReader();
                    userTable.Load(reader);
                }
            }

            return userTable;
        }

        private void btnSaveData_Click(object sender, EventArgs e)
        {
            var updateCommand = string.Empty;
            if (EditMode)
            {
                updateCommand = $"UPDATE Users SET Name = '{txtFullName.Text}', Email ='{txtEmail.Text}', BirthDate = '{dtpBirthDate.Value.ToShortDateString()}' WHERE Id = {CurrentId}";                
            }
            else
            {
                updateCommand = $"INSERT INTO Users(Name,RolId,BirthDate,Email,DateAdded) VALUES ('{txtFullName.Text}',{cbxRoles.SelectedIndex},'{dtpBirthDate.Value}','{txtEmail.Text}','{DateTime.Now}')";
            }

            RunSingleQuery(updateCommand);
            UpdateControls(false);
            ShowUserList();
        }

        private void RunSingleQuery(string command)
        {
            using (SqlConnection connection = new SqlConnection(GlobalConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(command, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            Int32 selectedRowCount =
               dgvUsers.Rows.GetRowCount(DataGridViewElementStates.Selected);
            if (selectedRowCount > 0)
            {
                EditMode = true;
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                RunSingleQuery($"UPDATE Users SET IsActive = 0 WHERE Id ={(int)dgvUsers.SelectedRows[0].Cells["Id"].Value}");
                ShowUserList();
            }
        }
    }
}
