/**
 * @file DetailsViewController.cs
 * @brief Manages the presentation logic for the details view ListView control.
 * @author (Github) IZH318 (https://github.com/IZH318)
 *
 * @details
 * This class acts as a controller in a pseudo-MVC pattern for WinForms. Its primary responsibility
 * is to decouple the logic for updating the details view from the main form. It receives data objects
 * and formats them for display in the associated ListView control, including grouping related properties.
 *
 * Key Features:
 *  - Decouples UI logic from the main application form.
 *  - Updates the details view based on a selected NodeData object.
 *  - Automatically groups related properties for improved readability.
 *  - Clears the view when no item is selected to prevent stale data display.
 *
 * Technical Environment:
 *  - Target Framework: .NET Framework 4.8
 *  - Last Update: 2025-12-24
 */

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace FSB_BANK_Extractor_Rebuilder_CS_GUI
{
    /// <summary>
    /// Manages the logic for populating and updating the details ListView.
    /// </summary>
    public class DetailsViewController
    {
        #region Constants

        // The separator used to parse property strings (Format: "Key: Value").
        private const char PROPERTY_SEPARATOR = ':';

        // Limit the split operation to 2 parts (Name and Value) to handle values that might contain colons.
        private const int MAX_SPLIT_PARTS = 2;

        #endregion

        #region Fields

        /// <summary>
        /// The ListView control that this controller manages.
        /// </summary>
        private readonly ListView _detailsListView;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DetailsViewController"/> class.
        /// </summary>
        /// <param name="detailsListView">The ListView control where details will be displayed.</param>
        public DetailsViewController(ListView detailsListView)
        {
            _detailsListView = detailsListView;

            // Initialize the ListView state to ensure it starts empty.
            _detailsListView.Items.Clear();
            _detailsListView.Groups.Clear();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the details view based on the selected NodeData object.
        /// </summary>
        /// <param name="selection">The NodeData object containing the data to display. If null, the view is cleared.</param>
        public void UpdateDetails(NodeData selection)
        {
            // Handle the case where no node is selected by clearing the view.
            // This prevents the user from seeing stale details from a previously selected item.
            if (selection == null)
            {
                _detailsListView.Items.Clear();
                return;
            }

            // Suspend layout updates to prevent UI flickering and improve performance during bulk insertion.
            _detailsListView.BeginUpdate();
            _detailsListView.Items.Clear();
            _detailsListView.Groups.Clear();

            // Retrieve the structured details key-value pairs from the selected data node.
            List<KeyValuePair<string, string>> details = selection.GetDetails();

            // Iterate through the details and format them for display in the ListView.
            foreach (var detail in details)
            {
                string groupName = detail.Key;

                // Split the value string into property name and value using the defined separator.
                // We use a predefined separator constant to ensure consistency with the NodeData format.
                string[] parts = detail.Value.Split(new[] { PROPERTY_SEPARATOR }, MAX_SPLIT_PARTS);

                string propName = parts[0].Trim();

                // Handle cases where the property value might be missing or empty.
                string propValue = parts.Length > 1 ? parts[1].Trim() : "";

                AddDetailItem(groupName, propName, propValue);
            }

            // Adjust column widths to fit the new content and resume layout logic.
            _detailsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            _detailsListView.EndUpdate();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds a single, grouped item to the ListView.
        /// </summary>
        /// <param name="groupName">The header of the group for this item.</param>
        /// <param name="propName">The name of the property being displayed.</param>
        /// <param name="value">The value associated with the property.</param>
        private void AddDetailItem(string groupName, string propName, string value)
        {
            // Ensure the item is categorized correctly by locating the target group.
            // If the group does not exist yet, create it dynamically to support arbitrary categories.
            ListViewGroup grp = _detailsListView.Groups.Cast<ListViewGroup>().FirstOrDefault(x => x.Header == groupName);
            if (grp == null)
            {
                grp = new ListViewGroup(groupName, groupName);
                _detailsListView.Groups.Add(grp);
            }

            // Create the new ListViewItem, assign it to the identified group, and add it to the view.
            var item = new ListViewItem(new[] { groupName, propName, value }) { Group = grp };
            _detailsListView.Items.Add(item);
        }

        #endregion
    }
}