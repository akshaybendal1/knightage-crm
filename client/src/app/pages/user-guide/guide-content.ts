export interface GuideSection {
  id: string;
  title: string;
  /** Font Awesome icon class, e.g. 'fa-user-plus'. */
  icon: string;
  /** Where to find it in the app, e.g. "Sidebar → Leads". */
  where?: string;
  summary: string;
  steps?: string[];
  tips?: string[];
  /** Who can use it, when not everyone. */
  roles?: string;
}

export interface Guide {
  appName: string;
  product: string;
  homeUrl: string;
  title: string;
  intro: string;
  searchExample: string;
  quickStart: string[];
  sections: GuideSection[];
  faqs: { q: string; a: string }[];
}

// Strings may contain simple HTML (<b>, <i>, <code>): they are written here, never typed in by users.
export const GUIDE: Guide = {
  appName: 'Knightage CRM',
  product: 'CRM',
  homeUrl: '/dashboard',
  title: 'How to use Knightage CRM',
  intro:
    'Everything you can do in the CRM, screen by screen: capturing leads, moving them through your pipeline, following up, ' +
    'sending quotations, turning won deals into customers, and running a field sales team.',
  searchExample: 'import, WhatsApp, quotation',
  quickStart: [
    'Open the login page and choose <b>Create an organization</b>. Enter your organization name, your name, email and a password. You become the organization’s <b>Owner</b>.',
    'A short product tour starts on the Dashboard. You can replay it any time with the <b>?</b> button at the top of the screen.',
    'Check your stages under <b>Pipeline → Pipeline and Product Stages</b>. New organizations start with New, Contacted, Qualified, Proposal Sent, Won and Lost.',
    'Add your first leads on <b>Leads</b>: one at a time with <b>+ New Lead</b>, or many at once with <b>Bulk import (CSV)</b>.',
    'Invite your team from <b>Settings → Team</b> with <b>+ Add Teammate</b>, and choose what each role can see under <b>Settings → Access Control</b>.',
  ],
  sections: [
    {
      id: 'dashboard',
      title: 'Dashboard',
      icon: 'fa-tachometer-alt',
      where: 'Sidebar → Dashboard',
      summary: 'Your day at a glance: how many leads sit in each stage (by count or by value), won and lost deals, this week’s activity, overdue tasks, leads stuck in a stage, and your upcoming tasks and meetings.',
      tips: ['Switch the pipeline overview between <b>Count</b> and <b>Value</b>, and the Won / Lost card between this week, this month and all time.'],
    },
    {
      id: 'leads',
      title: 'Leads',
      icon: 'fa-user-plus',
      where: 'Sidebar → Leads',
      summary: 'Every prospect your team is working on. Filter by stage, product or owner, and search by name, email, phone or company.',
      steps: [
        'Click <b>+ New Lead</b>. Enter the name (required), email, phone, company and notes.',
        'Pick the <b>Pipeline stage</b> (or <b>+ Add new stage…</b> to create one on the spot), the <b>Owner</b>, and optionally a <b>Product</b> the lead is interested in.',
        'Click <b>Add lead</b>. Use <b>View</b> to open the lead, <b>Edit</b> to change it, or <b>Delete</b> to remove it.',
        'To move several leads at once, tick them in the list, choose <b>Move to stage…</b> and click <b>Move</b>. You can add a note that is saved on each lead’s timeline.',
        'Click <b>Export</b> to download the leads you are looking at (with your filters) as an Excel file.',
      ],
      tips: ['A lead moved to a <b>Won</b> stage automatically becomes a customer (see Customers).'],
    },
    {
      id: 'import',
      title: 'Importing leads from a file',
      icon: 'fa-file-import',
      where: 'Leads → Bulk import (CSV)',
      summary: 'Bring in a list of leads from Excel or another CRM in one go.',
      steps: [
        'Click <b>Download template</b> to get a file with the right columns: <code>Name, Email, Phone, Company</code>. Only Name is required.',
        'Fill it in (or save your own sheet as CSV with the same columns). Values that contain commas must be in quotes, which Excel does for you.',
        'Choose the file, pick the <b>Pipeline stage</b> the leads should start in, and click <b>Import</b>.',
        'You will see how many leads were imported and which lines were skipped and why: a missing name, an invalid email, or a value longer than the form allows.',
      ],
      tips: ['Imported leads are owned by you and marked with the source <b>Import</b>. Importing straight into a Won stage also creates the customers.'],
    },
    {
      id: 'lead-detail',
      title: 'Working a lead: timeline, tasks, meetings and products',
      icon: 'fa-id-card',
      where: 'Leads → View',
      summary: 'One page with everything about a lead: the activity timeline, tasks, meetings, the products being discussed, and documents sent.',
      steps: [
        'Write in <b>Add a note</b> after every call or visit. Notes, stage changes, tasks, meetings and documents all appear on the lead’s <b>Activity timeline</b>.',
        'Under <b>Tasks</b>, add a follow-up with a title, due date and who it is assigned to. Tick <b>Complete</b> when it is done.',
        'Under <b>Meetings</b>, schedule a meeting with start and end time and a location.',
        'Under <b>Products</b>, add the products and quantities the lead wants. Each product line can have its own stage (for example Sample sent → Order placed).',
        'Use <b>Move</b> to change the lead’s stage. Add a note to say why; it is saved on the timeline.',
      ],
    },
    {
      id: 'documents',
      title: 'Quotations and invoices',
      icon: 'fa-file-invoice',
      where: 'Lead page → Documents → New document',
      summary: 'Create a branded PDF quotation or invoice from the lead’s product lines: your logo, itemised products, discount and tax.',
      steps: [
        'Add the products to the lead first (see Working a lead).',
        'Under <b>Documents</b>, choose <b>New document</b>, pick <b>Quotation</b> or <b>Invoice</b>, and set the <b>Discount (%)</b> and <b>Tax rate (%)</b>.',
        '<b>Download</b> the PDF, or click <b>Send</b> to email it to the lead (and <b>Resend</b> later if needed). Its status moves from Draft to Sent.',
        'When the customer pays, click <b>Mark paid</b>. Every step is recorded on the lead’s timeline.',
      ],
      tips: ['Upload your logo once under <b>Settings → Organization</b> and it appears on every document.'],
    },
    {
      id: 'whatsapp',
      title: 'WhatsApp messages and templates',
      icon: 'fa-comment-dots',
      where: 'Lead page or Leads list → WhatsApp; Settings → Message Templates',
      summary: 'Message one lead, or a whole selection, on WhatsApp using ready-made templates.',
      steps: [
        'Create templates under <b>Settings → Message Templates</b> with <b>+ New Template</b>. Use <code>{{leadName}}</code> and <code>{{yourName}}</code> and they are filled in for each lead.',
        'On a lead, click the WhatsApp button, choose a <b>Template</b> or write a <b>Custom message</b>, optionally attach a file, and send. WhatsApp opens with the message ready.',
        'To message many leads, tick them in the list and choose <b>Send WhatsApp messages</b>. <b>Review &amp; Send</b> walks you through them one by one.',
      ],
      tips: ['Only leads with a phone number can be messaged.'],
    },
    {
      id: 'timeline',
      title: 'Timeline board',
      icon: 'fa-columns',
      where: 'Sidebar → Pipeline → Timeline',
      summary: 'Your pipeline as columns, one per stage. Switch between <b>Leads</b> and <b>Lead Products</b>.',
      steps: [
        'Drag a card from one column to the next to change its stage.',
        'To move several at once, select them and use <b>Move selected</b>.',
        'Filter the board by lead, product or owner.',
      ],
    },
    {
      id: 'stages',
      title: 'Pipeline and product stages',
      icon: 'fa-stream',
      where: 'Sidebar → Pipeline → Pipeline and Product Stages',
      summary: 'Set up the steps your deals go through, and separately the steps a product line goes through.',
      steps: [
        'Add, rename and reorder stages with <b>Sort order</b>.',
        'Mark the stages that mean a deal is finished: <b>Counts as Won</b> and <b>Counts as Lost</b>. Won and lost reports and the automatic customer creation use these.',
        'Choose the <b>Default for sales entries</b> stage used by field sales visits.',
      ],
      roles: 'Owner and Admin by default (Access Control can change this).',
    },
    {
      id: 'tasks',
      title: 'Tasks and calendar',
      icon: 'fa-tasks',
      where: 'Sidebar → Tasks and Calendar',
      summary: '<b>Tasks</b> lists your open follow-ups; the bell at the top shows how many are due. <b>Calendar</b> shows tasks by date.',
      steps: [
        'Create tasks from a lead’s page so they stay linked to the lead.',
        'On <b>Calendar</b>, switch between <b>My tasks</b> and <b>Whole team</b> (Owner and Admin), and use <b>Today</b> to jump back to this month.',
      ],
    },
    {
      id: 'customers',
      title: 'Customers',
      icon: 'fa-handshake',
      where: 'Sidebar → Customers',
      summary: 'The people and companies who have bought from you. A lead becomes a customer when it is moved to a Won stage.',
      steps: [
        'Add a customer by hand with <b>+ New Customer</b>.',
        'Won leads from before this feature, or ones you skipped, are listed under <b>Won leads that are not customers yet</b>. Click <b>Choose leads</b>, tick the ones you want and click <b>Convert selected</b>, or click <b>Convert all</b>.',
        'If your organization also uses <b>Knightage Inventory</b>, new customers are sent there automatically. The <b>Inventory</b> column shows <b>In Inventory</b> or <b>Not sent yet</b>; <b>Send to Inventory</b> sends any that are waiting. Customers added in Inventory also appear here.',
      ],
      roles: 'Converting leads is for the Owner and Admin.',
    },
    {
      id: 'products',
      title: 'Products and categories',
      icon: 'fa-box-open',
      where: 'Sidebar → Catalog → Products / Product Categories',
      summary: 'The catalog your leads and documents pull from: name, description, category and price.',
      steps: [
        'Create categories first under <b>Product Categories</b> (with a sort order), then add products with <b>+ New Product</b>.',
        'Deactivate a product you no longer sell instead of deleting it; existing leads keep it.',
        'In the lead form you can also add a product on the spot with <b>+ Add new product</b>.',
      ],
    },
    {
      id: 'reports',
      title: 'Reports and custom reports',
      icon: 'fa-chart-line',
      where: 'Sidebar → Insights → Reports',
      summary: 'Lead volume over time, lead sources, team performance, task trends, and leads by product, category and owner, for this week, this month, all time or a custom range. Everything exports to Excel.',
      steps: [
        'Pick the period and click <b>Apply</b>.',
        'Click <b>+ Custom Report</b> to build your own: choose filters (stage, owner, product, category, dates) and columns, save it, and run it again any time.',
      ],
      roles: 'Owner and Admin by default; the Owner can grant Reports to other roles in Access Control.',
    },
    {
      id: 'field-sales',
      title: 'Field sales visits',
      icon: 'fa-map-marker-alt',
      where: 'Sidebar → Field Sales (Add Sales Entry, Sales Dashboard, Sales Coverage)',
      summary: 'For reps on the road: log a doorstep visit with the customer, products and quantities. The entry is saved with the phone’s real GPS location.',
      steps: [
        'Open <b>Add Sales Entry</b>, choose or type the customer, add products and quantities, and submit. Allow location access when asked; the form will not submit without it.',
        '<b>Sales Dashboard</b> lists entries with count, value and location; click <b>View on map</b>.',
        '<b>Sales Coverage</b> shows where each rep has been by day, week or month.',
      ],
      tips: ['Field sales is switched on per organization by Knightage. Ask through <b>Contact Us</b> if you need it.'],
      roles: 'The <b>Sales</b> role sees only the field sales screens. Owner and Admin see the coverage map.',
    },
    {
      id: 'team',
      title: 'Team and roles',
      icon: 'fa-users',
      where: 'Sidebar → Settings → Team',
      summary: 'Add the people in your organization and give each one a role.',
      steps: [
        'Click <b>+ Add Teammate</b>, enter their name, email, phone, role and an <b>Initial password</b>, and share the password with them. They can change it from <b>My Profile</b>.',
        'Use <b>Edit role</b> to change a role, and <b>Deactivate</b> someone who has left (they can no longer sign in; <b>Reactivate</b> brings them back).',
      ],
      tips: [
        '<b>Owner</b>: everything, including Access Control. <b>Admin</b>: runs the CRM day to day. <b>Member</b>: works on leads and tasks. <b>Sales</b>: field sales only.',
        'The number of users may be limited by your plan.',
      ],
      roles: 'Owner and Admin.',
    },
    {
      id: 'access-control',
      title: 'Access control',
      icon: 'fa-user-shield',
      where: 'Sidebar → Settings → Access Control',
      summary: 'Choose which screens the Admin, Member and Sales roles can open. The Owner always sees everything.',
      steps: [
        'Tick or untick screens for each role. Changes apply the next time each person loads a page.',
        'Use <b>Discard</b> to undo unsaved changes, or <b>Reset to defaults</b> to go back to the built-in settings.',
      ],
      roles: 'Owner (and anyone the Owner gives this screen to).',
    },
    {
      id: 'organization',
      title: 'Organization settings and subscription',
      icon: 'fa-building',
      where: 'Sidebar → Settings → Organization',
      summary: 'Your organization’s name, contact email, phone and address, and your logo (PNG or JPEG, up to 2 MB), shown in the sidebar, the browser tab and on documents.',
      tips: [
        'The banner at the top tells you if your subscription is not set up, in its grace period (view only) or expired. Use <b>Request one</b> to ask Knightage for a plan.',
      ],
    },
    {
      id: 'profile',
      title: 'My profile, password and account',
      icon: 'fa-user-cog',
      where: 'Your name at the top right → My Profile',
      summary: 'Update your name and phone, and <b>Change password</b>.',
      tips: [
        'Forgot your password? Use <b>Forgot password</b> on the login page and follow the email.',
        '<b>Delete account</b> removes your own login. To close the whole organization, the Owner follows <b>How to delete the organization</b>.',
      ],
    },
    {
      id: 'apps',
      title: 'Other Knightage apps and the Android app',
      icon: 'fa-th-large',
      where: 'Sidebar → Apps',
      summary: 'Open <b>Inventory</b>, <b>Accounting</b> or the <b>Platform</b> straight from the sidebar; you stay signed in. Customers are shared between the CRM and Inventory.',
      tips: ['The CRM is also an Android app. Sign in with the same email and password. You can switch between light and dark themes with the sun / moon button.'],
    },
  ],
  faqs: [
    { q: 'Why can’t I see a screen my colleague can see?', a: 'Each role sees the screens ticked for it under <b>Settings → Access Control</b>. Ask your Owner to change it.' },
    { q: 'Why did my imported leads not all come in?', a: 'Lines without a name, with an invalid email, or with a value longer than the form allows are skipped. The import result lists each skipped line and the reason. Fix those lines and import just them again.' },
    { q: 'Why is my lead not a customer even though it is Won?', a: 'Only stages marked <b>Counts as Won</b> create customers. Check the stage under Pipeline and Product Stages, then use <b>Customers → Choose leads</b> to convert it.' },
    { q: 'Why can I view but not change anything?', a: 'Your subscription is in its grace period or has expired. The banner at the top says which. Contact your Owner or Knightage to renew.' },
    { q: 'Is my data separate from other companies using Knightage?', a: 'Yes. Every record belongs to your organization, and other organizations cannot see or change it.' },
  ],
};
