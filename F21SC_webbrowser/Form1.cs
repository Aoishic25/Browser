using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace F21SC_webbrowser
{
    public partial class Form1 : Form
    {
        private bool isNavigating = false;
        private DatabaseManager dbm;
        public int? currentUserID = null;
        public string currentUsername = null;

        public Form1()
        {
            InitializeComponent();

            dbm = new DatabaseManager(); // Initialize DatabaseManager
            InitializeWebView2(); // Initialize WebView2

            // Set up the double-click event handler for links only once
            listBoxLinks.DoubleClick += ListBoxLinks_DoubleClick;
        }

        private async void InitializeWebView2()
        {
            try
            {
                // Initialize WebView2
                await webView2.EnsureCoreWebView2Async(null);

                // Set modern user agent
                webView2.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0";

                // Enable modern web features
                webView2.CoreWebView2.Settings.IsWebMessageEnabled = true;
                webView2.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                webView2.CoreWebView2.Settings.AreDevToolsEnabled = true;
                webView2.CoreWebView2.Settings.AreHostObjectsAllowed = true;
                webView2.CoreWebView2.Settings.IsZoomControlEnabled = true;
                webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;

                // Handle navigation events
                webView2.NavigationCompleted += WebView2_NavigationCompleted;
                webView2.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
                webView2.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;

                // Navigate to homepage or default page
                if (currentUsername != null)
                {
                    string homepage = dbm.GetHomepageForCurrentUser();
                    if (!string.IsNullOrEmpty(homepage))
                    {
                        webView2.CoreWebView2.Navigate(homepage);
                        return;
                    }
                }
                else
                {
                    // Navigate to a default page
                    string homeUrl = "https://www.hw.ac.uk/";
                    webView2.CoreWebView2.Navigate(homeUrl);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize WebView2: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Forward
            if (webView2.CoreWebView2 != null && webView2.CoreWebView2.CanGoForward)
            {
                webView2.CoreWebView2.GoForward();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Back
            if (webView2.CoreWebView2 != null && webView2.CoreWebView2.CanGoBack)
            {
                webView2.CoreWebView2.GoBack();
            }
        }

        //Search button
        private void button1_Click(object sender, EventArgs e)
        {
            string url = textBox1.Text.Trim();

            // Validate URL format
            if (string.IsNullOrWhiteSpace(url))
            {
                ShowErrorPage("400 Bad Request", "The URL cannot be empty.");
                return;
            }

            // Add protocol if missing
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                url = "https://" + url;
            }

            // Validate URL format
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult) ||
                (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                ShowErrorPage("400 Bad Request", "The URL entered is invalid or malformed.");
                return;
            }

            if (webView2.CoreWebView2 != null)
            {
                webView2.CoreWebView2.Navigate(url);
            }
        }

        //Refresh
        private async void button4_Click(object sender, EventArgs e)
        {
            if (webView2.CoreWebView2 != null)
            {
                try
                {
                    await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync(
                        "Network.clearBrowserCache", "{}");
                    await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync(
                        "Network.clearBrowserCookies", "{}");

                    webView2.CoreWebView2.Reload();
                }
                catch
                {
                    webView2.CoreWebView2.Reload();
                }
            }
        }

        private async void WebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            string url = webView2.CoreWebView2.Source;

            if (e.IsSuccess)
            {
                // Navigate successful → treat as 200 OK
                if (!string.IsNullOrEmpty(url) && !isNavigating)
                {
                    dbm.AddHistoryForCurrentUser(webView2.CoreWebView2.DocumentTitle, url);
                }

                // Update window title
                string title = webView2.CoreWebView2.DocumentTitle ?? "Untitled Page";
                this.Text = $"{title} - {url} (200 OK)";

                // Harvest links
                LinkHarvester harvester = new LinkHarvester();
                var links = await harvester.GetFiveLinksAsync(webView2.CoreWebView2);
                DisplayHarvestedLinks(links);
            }
            else
            {
                // Handle navigation failure
                this.Text = "--";
                string errorMessage = "An error occurred while loading the page.";
                string errorCode = "Navigation Failed";

                if (e.WebErrorStatus == CoreWebView2WebErrorStatus.HostNameNotResolved)
                {
                    errorCode = "404 Not Found";
                    errorMessage = "The server could not be found. Please check the URL and try again.";
                }
                else if (e.WebErrorStatus == CoreWebView2WebErrorStatus.ConnectionAborted)
                {
                    errorCode = "400 Bad Request";
                    errorMessage = "Connection was aborted. The server may be down or the URL is incorrect.";
                }
                else if (e.WebErrorStatus == CoreWebView2WebErrorStatus.Unknown)
                {
                    errorCode = "404 Not Found";
                    errorMessage = "The domain name could not be resolved or the server is unreachable.";
                }

                // Update the window title to show the error code (Not 200 OK)
                this.Text = $"{errorCode} - {url}";
            }
        }


        //Helper method for harvesting links
        private void DisplayHarvestedLinks(List<(string href, string text)> links)
        {
            listBoxLinks.Items.Clear();
            if (links == null || links.Count == 0)
            {
                listBoxLinks.Items.Add("No links found.");
                return;
            }
            foreach (var link in links)
            {
                listBoxLinks.Items.Add($"{link.text} ({link.href})");
            }
        }

        //go to default homepage after logging out
        public void NavigateToDefaultHomepage()
        {
            string defaultHome = "https://www.hw.ac.uk";
            webView2.Source = new Uri(defaultHome);
        }

        private void ListBoxLinks_DoubleClick(object sender, EventArgs e)
        {
            if (listBoxLinks.SelectedItem != null)
            {
                // Extract the URL part
                string itemText = listBoxLinks.SelectedItem.ToString();
                int startIndex = itemText.LastIndexOf("(");
                int endIndex = itemText.LastIndexOf(")");
                if (startIndex != -1 && endIndex > startIndex)
                {
                    string selectedUrl = itemText.Substring(startIndex + 1, endIndex - startIndex - 1);
                    webView2.CoreWebView2.Navigate(selectedUrl);
                }
            }
        }

        // Helper method to get raw HTML
        private async Task<string> GetRawHtmlAsync(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    string html = await response.Content.ReadAsStringAsync();
                    return html;
                }
            }
            catch (Exception ex)
            {
                return $"Error retrieving HTML:\n{ex.Message}";
            }
        }

        // Display raw HTML in a new window
        private void ShowRawHtml(string html, string status)
        {
            Form rawHtmlForm = new Form
            {
                Text = $"Raw HTML - {status}",
                Size = new System.Drawing.Size(800, 600),
                StartPosition = FormStartPosition.CenterParent
            };

            RichTextBox htmlBox = new RichTextBox
            {
                Text = html,
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Consolas", 10),
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };

            rawHtmlForm.Controls.Add(htmlBox);
            rawHtmlForm.ShowDialog(this);
        }

        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            isNavigating = true;
        }

        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            if (webView2.CoreWebView2 != null)
            {
                string title = webView2.CoreWebView2.DocumentTitle;
                string url = webView2.CoreWebView2.Source;
                this.Text = $"{title} - {url}";
                isNavigating = false;
            }
        }

        private void ShowErrorPage(string errorCode, string errorMessage)
        {
            string htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{errorCode}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; background-color: #f5f5f5; }}
        .error-container {{ background-color: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .error-code {{ color: #d32f2f; font-size: 24px; font-weight: bold; margin-bottom: 10px; }}
        .error-message {{ color: #666; font-size: 16px; line-height: 1.5; }}
        .error-icon {{ font-size: 48px; color: #d32f2f; margin-bottom: 20px; }}
    </style>
</head>
<body>
    <div class='error-container'>
        <div class='error-icon'>⚠️</div>
        <div class='error-code'>{errorCode}</div>
        <div class='error-message'>{errorMessage}</div>
        <br>
        <p><strong>Suggestions:</strong></p>
        <ul>
            <li>Check if the URL is spelled correctly</li>
            <li>Make sure the website is accessible</li>
            <li>Try refreshing the page</li>
        </ul>
    </div>
</body>
</html>";

            if (webView2.CoreWebView2 != null)
            {
               // webView2.CoreWebView2.NavigateToString(htmlContent);
                this.Text = $"{errorCode} - Error";
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // Add Bookmark button
            if (webView2.CoreWebView2 != null && !string.IsNullOrEmpty(webView2.CoreWebView2.Source))
            {
                string title = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter bookmark title:",
                    "Add Bookmark",
                    webView2.CoreWebView2.DocumentTitle);

                if (!string.IsNullOrWhiteSpace(title))
                {
                    //handles duplicates internally
                    dbm.AddBookmarkForCurrentUser(title, webView2.CoreWebView2.Source);
                    MessageBox.Show("Bookmark added successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Bookmark title cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("No page loaded to bookmark.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            // View Bookmarks button
            var bookmarks = dbm.GetBookmarks();
            if (bookmarks.Count == 0)
            {
                MessageBox.Show("No bookmarks available.", "No Bookmarks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Show bookmarks window
            Form bookmarksForm = new Form
            {
                Text = "Bookmarks",
                Size = new Size(400, 350),
                StartPosition = FormStartPosition.CenterParent
            };

            ListBox lb = new ListBox { Dock = DockStyle.Top, Height = 227, Font = new Font("Arial", 10) };
            foreach (var bm in bookmarks)
                lb.Items.Add($"{bm.Name} ({bm.Url})");

            // Open button
            Button openButton = new Button { Text = "Open", Dock = DockStyle.Bottom, Height = 30 };
            openButton.Click += (s, ev) =>
            {
                if (lb.SelectedIndex >= 0)
                {
                    string url = bookmarks[lb.SelectedIndex].Url;
                    if (webView2.CoreWebView2 != null)
                        webView2.CoreWebView2.Navigate(url);
                    bookmarksForm.Close();
                }
                else
                {
                    MessageBox.Show("Select a bookmark to open.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            // Edit button
            Button editButton = new Button { Text = "Edit", Dock = DockStyle.Bottom, Height = 30 };
            editButton.Click += (s, ev) =>
            {
                if (lb.SelectedIndex >= 0)
                {
                    string currentTitle = bookmarks[lb.SelectedIndex].Name;
                    string newTitle = Microsoft.VisualBasic.Interaction.InputBox(
                        "Enter new title:", "Edit Bookmark", currentTitle);

                    if (string.IsNullOrWhiteSpace(newTitle))
                    {
                        MessageBox.Show("Bookmark title cannot be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    int id = bookmarks[lb.SelectedIndex].id;
                    dbm.UpdateBookmark(id, newTitle, bookmarks[lb.SelectedIndex].Url);

                    // Update listbox
                    bookmarks[lb.SelectedIndex] = (id, newTitle, bookmarks[lb.SelectedIndex].Url);
                    lb.Items[lb.SelectedIndex] = $"{newTitle} ({bookmarks[lb.SelectedIndex].Url})";
                }
                else
                {
                    MessageBox.Show("Select a bookmark to edit.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };

            // Delete button
            Button deleteButton = new Button { Text = "Delete", Dock = DockStyle.Bottom, Height = 30 };
            deleteButton.Click += (s, ev) =>
            {
                if (lb.SelectedIndex >= 0)
                {
                    int id = bookmarks[lb.SelectedIndex].id;
                    dbm.DeleteBookmark(id);

                    bookmarks.RemoveAt(lb.SelectedIndex);
                    lb.Items.RemoveAt(lb.SelectedIndex);
                }
                else
                {
                    MessageBox.Show("Select a bookmark to delete.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            bookmarksForm.Controls.Add(lb);
            bookmarksForm.Controls.Add(openButton);
            bookmarksForm.Controls.Add(editButton);
            bookmarksForm.Controls.Add(deleteButton);
            bookmarksForm.ShowDialog(this);
        }


        //Show History button
        private void button7_Click(object sender, EventArgs e)
        {
            var history = dbm.GetHistory();
            if (history.Count == 0)
            {
                MessageBox.Show("No browsing history found.", "History", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Create the history window
            Form historyForm = new Form()
            {
                Text = "Browsing History",
                Size = new Size(600, 450),
                StartPosition = FormStartPosition.CenterParent
            };

            // Create controls
            ListBox lb = new ListBox() { Dock = DockStyle.Fill };
            Button clearBtn = new Button()
            {
                Text = "Clear History",
                Dock = DockStyle.Bottom,
                Height = 35
            };

            // Populate history items
            foreach (var h in history)
            {
                lb.Items.Add($"{h.VisitedAt:yyyy-MM-dd HH:mm} - {h.Title} ({h.Url})");
            }

            // Double-click to open a URL
            lb.DoubleClick += (s, args) =>
            {
                if (lb.SelectedItem != null)
                {
                    string itemText = lb.SelectedItem.ToString();
                    int start = itemText.LastIndexOf("(");
                    int end = itemText.LastIndexOf(")");
                    if (start != -1 && end > start)
                    {
                        string url = itemText.Substring(start + 1, end - start - 1);
                        webView2.CoreWebView2.Navigate(url);
                        historyForm.Close();
                    }
                }
            };

            // Clear history button logic
            clearBtn.Click += (s, args) =>
            {
                var confirm = MessageBox.Show(
                    "Are you sure you want to clear all browsing history?",
                    "Confirm Clear History",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    dbm.ClearHistory();
                    lb.Items.Clear();
                    MessageBox.Show("Browsing history cleared.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            // Add controls to form
            historyForm.Controls.Add(lb);
            historyForm.Controls.Add(clearBtn);
            historyForm.ShowDialog(this);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            //Setting new homepage
            if (webView2.CoreWebView2 != null && !string.IsNullOrEmpty(webView2.CoreWebView2.Source))
            {
                string currentUrl = webView2.CoreWebView2.Source;
                DialogResult confirm = MessageBox.Show(
                    $"Set this page as your homepage?\n\n{currentUrl}",
                    "Set Homepage",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    dbm.SetHomepageForCurrentUser(currentUrl);
                }
            }
            else
            {
                MessageBox.Show("No page loaded to set as homepage.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //Home button
        private async void button8_Click(object sender, EventArgs e)
        {
            if (webView2.CoreWebView2 == null) return;
            try
            {
                await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync(
                    "Network.clearBrowserCache", "{}");
            }
            catch { }
            string homepageUrl = "https://www.hw.ac.uk/"; //default homepage
            if (currentUsername != null && currentUserID != null)
            {
                string userHomepage = dbm.GetHomepageForCurrentUser();
                if (!string.IsNullOrEmpty(userHomepage))
                {
                    homepageUrl = userHomepage;
                }
            }
            webView2.CoreWebView2.Navigate(homepageUrl); // Navigate to homepage
        }

        private void homeCtrlHToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button8_Click(sender, e); //Reuse existing method
        }

        private void refreshCtrlRToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button4_Click(sender, e);
        }

        private void addBookmarkCtrlDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button5_Click(sender, e);
        }

        private void viewBookmarksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button6_Click(sender, e);
        }

        private void viewHistoryCtrlShiftHToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button7_Click(sender, e);
        }

        //Sign In/Sign Up button
        private void button9_Click(object sender, EventArgs e)
        {
            using (var loginForm = new LoginForm(dbm,this)) // Pass DatabaseManager to LoginForm
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    currentUserID = dbm.CurrentID;
                    currentUsername = dbm.CurrentUsername;

                    MessageBox.Show($"Welcome, {currentUsername}!", "Login Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    labelUserGreeting.Text = $"Hello {currentUsername}";
                    labelUserGreeting.Visible = true;
                    labelUserGreeting.BringToFront();

                    // Immediately load the user’s homepage after login
                    string homepage = dbm.GetHomepageForCurrentUser();

                    if (!string.IsNullOrEmpty(homepage))
                    {
                        if (webView2.CoreWebView2 != null)
                        {
                            webView2.CoreWebView2.Navigate(homepage);
                        }
                    }
                    else
                    {
                        // Fallback: use default homepage
                        webView2.CoreWebView2?.Navigate("https://www.hw.ac.uk/");
                    }
                }
            }
        }

        // Logout user method
        public void LogoutUser()
        {
            // Clear login session
            currentUserID = null;
            currentUsername = null;

            // Hide greeting label and logout button if any
            labelUserGreeting.Visible = false;

            // Navigate to default homepage
            NavigateToDefaultHomepage();

            // Optionally show a small confirmation here if needed
        }


        private async void showRawHTMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (webView2.CoreWebView2 == null)
                {
                    MessageBox.Show("The browser is not ready yet.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string url = webView2.CoreWebView2.Source;

                if (string.IsNullOrEmpty(url))
                {
                    MessageBox.Show("No webpage is currently loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Reuse your existing function to fetch raw HTML
                string rawHtml = await GetRawHtmlAsync(url);
                ShowRawHtml(rawHtml, url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching raw HTML: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}