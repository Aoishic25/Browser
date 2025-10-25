using F21SC_webbrowser;
using System;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.Text;

namespace F21SC_webbrowser
{
    public partial class  LoginForm : Form
    {
        private readonly DatabaseManager db;
        private readonly Form1 mainForm;

        public LoginForm(DatabaseManager databaseManager, Form1 form1)
        {
            db = databaseManager;
            mainForm = form1;
            BuildUI();
        }

        // Build the UI components
        private void BuildUI()
        {
            //creating the login form
            this.Text = "User Login";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 400;

            // Username Label and TextBox
            Label lblUser = new Label { Text = "Username:", Left = 20, Top = 25, Width = 80 };
            TextBox txtUser = new TextBox { Left = 110, Top = 20, Width = 200, Name = "txtUser" };

            // Password Label and TextBox
            Label lblPassword = new Label { Text = "Password:", Left = 20, Top = 65, Width = 80 };
            TextBox txtPassword = new TextBox { Left = 110, Top = 60, Width = 200, Name = "txtPassword", PasswordChar = '*' };

            // Login Button
            Button btnLogin = new Button { Text = "Login", Left=110,Top=110, Width=90 };
            Button btnRegister = new Button { Text = "Register", Left = 220, Top = 110, Width = 90 };
            Button btnLogout = new Button { Text = "Logout", Left = 20, Top = 110, Width = 90 };

            btnLogin.Click += (s, ev) =>
            {
                string username = txtUser.Text.Trim();
                string password = txtPassword.Text;

                // Validate input
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Please enter both username and password.", "Missing Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (db.LoginUser(username, password))
                {
                    MessageBox.Show("Login successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnRegister.Click += (s, ev) =>
            {
                string username = txtUser.Text.Trim();
                string password = txtPassword.Text;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Please enter both username and password.", "Missing Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (db.RegisterUser(username, password))
                {
                    MessageBox.Show("Registration successful! You can now log in.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Username already exists. Please choose a different username.", "Registration Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnLogout.Click += (s, ev) =>
            {
                db.LogoutUser();
                mainForm.LogoutUser(); // call Form1’s public logout method

                MessageBox.Show("You have been logged out.", "Logged Out", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            };

            this.Controls.Add(lblUser);
            this.Controls.Add(txtUser);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnLogin);
            this.Controls.Add(btnRegister);
            this.Controls.Add(btnLogout);

            this.ClientSize = new System.Drawing.Size(370, 150);

        }
    }
}