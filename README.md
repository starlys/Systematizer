# Systematizer

A Windows address book, calendar, and to-do list optimized for people whose executive function style is similar to the author. The author forgets stuff,
so it has to be recorded immediately in structured lists.

## Main features

Executive functioning main points:

* Everything is in a vertical list with the most important at the top.
* To-do items are interspersed with scheduled calendar items, rather than in separate lists.
* Nothing "goes away" without explicit user action.
* Today's schedule is shown in chunks so you can plan the day in parts like desk work, errands, house work, etc.
* Daily reminders like taking medications are not cluttery.
* One-click note taking so you can track interruptions and not lose the earlier thing or the interrupting thing.

Some other nice features:

* Formatted notes
* Four views for planning ahead: Today, Tomorrow, Agenda (appointments in the upcoming weeks at a glance) and Calendar (long term planning).
* People and tasks can be linked in several ways.
* People can be categorized hierarchically, so you can see a list of family, or people I met in France, or mechanics.
* Outline of notes to store accounts, vendor info, and other miscellany.
* Quick click from task to associated web sites with password management.
* Highlighted tasks are used for trips and other multi-day tasks, and you can plan the next months and years easily.
* Repeated tasks include several patterns and can support exceptions.
* File integration allows you to set up Windows folders and files that correspond with tasks/sub-tasks, so you can quickly open related files, or create them from templates.
* Email integration allows you to store emails in tasks to clean up inbox and defer dealing with those emails.
* Import/export

Some design points:

* This is offline first because you can't afford to have your calendar depend on the internet.
* Plan for mobile and other platforms (not implemented); each will use the same database format for compatibility.
* Multiple databases - you decide where to store them and are responsible for backups.
* It's open source - if you know or want to learn C# or WPF, you can contribute a fix or feature

## Installation

* [Download the Windows installer for version 1.1.6](Systematizer-1.1.6.msi?raw=true). 
* Windows will say the file is dangerous because we haven't done any of the security tasks for publishing software, so it's being helpful. You should only bypass the warning and run the installer if you trust the source, which in this case means you know the author. 
* If you run it and it requests to install the "NET Core runtime", then go ahead and download it as requested, but be sure to run the "x86" version.

## Really abbreviated user guide

### Keeping up to date

As of v1.0, it does not check for updates, so come back to the github site to see the latest version.

### Understanding the panes

The left side of the display has things that stay there all the time (Done, Today, Tomorrow, Agenda, Calendar and Subjects), and it also shows search results there. 
The right side shows task or person you have open. Each item can be collapsed - for example, "Tomorrow" is collapsed by default. You can expand any collapsed item by
clicking on it. If an item on the right side is left collapsed for a few minutes, it goes away, but there is no other close function.

The orange bar on the side of a pane indicates that it is active and keyboard commands will affect that pane. Use Ctrl-Arrows to activate the other panes.

Press Esc to save and collapse a pane. Press Ctrl-Enter to save and leave open, but this also ends edit mode. 
When panes have a dark background, they are not editable. Click the orange bar to go back to edit mode.

### Quick notes

The "quick note" button (or F12) creates a quick note without asking any questions. This is useful if you get a sudden interruption or a flash of insight that 
needs to be recorded. Quick notes stay on screen until you classify them, even if you close and reopen the app. They must have a title to be able to close the app,
however. They do not appear anywhere else.

### The "Today" pane

Today is divided into three chunks by default - Morning, Afternoon, and Evening. You can rename the chunks, delete them by clearing out the text, and add new chunks
by entering a name in the blank space at the end. Chunks are an optional organizing aid that only lasts for that day, and are not saved after that.

Until you move tasks to the afternoon and evening chunks, they appear in the morning. Drag the task to another chunk by dragging the time to the left of the task
description. Drag down until you see a red indicator of where it will be dropped. You can always drop a task on the chunk title, but you cannot always move a task under 
another task. The reason for that limitation is that it doesn't let you put scheduled tasks out of order.

You can click on the time to open the task. Or you can use the keyboard: tab to select the task title, then press Enter to open.

To create a new task and have it be filed in the currently selected chunk, use Ctrl-B (new sub-item).

### The "Tomorrow" pane

The purpose of the Tomorrow pane is just like Today, to be used at the end of the day if you want to pre-plan the next day. When Tomorrow is open, the Agenda
pane does not show that date, so that tasks are not shown in both places.

### Kinds of tasks

In the task or note pane, there are 3 dropdown selections: schedule type, visibility, and importance.

* Schedule type is used to specify how accurately the task is scheduled:
  * Not scheduled: Use this for notes that are not tasks at all.
  * Approximate: Use this when you need to do something around a date, but not necessarily on that exact day.
  * Day: Use this when you need to do something at any time on a day.
  * Exact time: Use this for appointments.

* Visibility
  * Low clutter: Use this for tasks that you will never plan other things around because they are quick, such as taking medicine or feeding your seven cats.
  * Normal visibility: The default for tasks not scheduled for an exact time
  * Plan around: The default for tasks scheduled at an exact time that you would need to plan other things around (these are shown on the Agenda)
  * Highlight: Use this to give the task a bright color - more details below.

* Importance
  * Optional: Use this for tuning into a broadcast or something that you do not necessarily need to do. If missed, it will automatically mark it as done.
  * Normal: The default (most things)
  * Keep-alive: Use this for things that it would be terrible to miss. It forces an extra step to mark the task as done.

This may seem confusing, but it's helpful to distinguish these three categorizations to keep the clutter down without losing sight of the important things.
Visibility controls how you plan around the thing - where it shows up. Importance controls how easy it is to complete the task. Schedule type controls how
it works with time and date.

### The "Agenda" pane

Use Agenda to plan the next weeks. For example if you need to find out if you are free at a certain time, open the Agenda. Click See More to expand beyond
the initial two weeks. The slider at the top is rarely used but you can use it to show or hide things based on their visibility.

### The "Calendar" pane and highlights

Use Calendar to see weeks and months, for the next six months or more. It will show at least through your last scheduled task. This is good for planning
trips and other big things.

Nothing appears on the calendar unless you use the highlight option for visibility. For example if you are going on a week-long trip, set that task to highlight, and set the 
duration to "7d" (7 days). When it is saved, a bar spanning the 7 days shows on the calendar. You can click the bar to reopen the task.

If you have many highlighted items, the system chooses different colors so they can overlap. The colors show on the Agenda pane as well.

### Entering dates and times

Dates have an unusual way of interacting, but they are done for maximum speed with the keyboard. They show a blank entry box, then a calendar icon,
then the date in a format like "2020-12-31 - Mon". Here is how to set dates:

* Press a day-of-week letter to advance the date to that day of the week. S=Sunday, M=Monday, and so on, except two special cases: H=Thursday, and A=Saturday.
* Type the 4-digit year and tab out to change the year while leaving the month and day the same.
* Type the day only (1-31) then tab out to advance to the next day (which is sometimes in the following month).
* Type the month and day (separateed by a space or slash or period) then tab out to advance to that month and day, in the same year or the next year.
* Type the year, month, and day then tab out
* Click the calendar icon and select the date there. Within the drop-down calendar you can navigate months (left, right arrows) or click the month name to jump to other months. In rare cases you can keep clicking the title to back out to years and decades.

Times show up in 24-hour format (such as 14:00). To change the time:

* Type just the hour and tab out to use a time on the hour
* Type the hour and two-digit minute together, such as "1615". Or use punctuation (space or colon or dot) between the hour and minute.

Durations are a special type of entry that is only used for tasks shceduled for an exact time. You enter the duration or prep duration like this:

* Minutes: a number followed by "m". Example: 15m
* Hours: a number followed by "h". Example: 3h
* Days: a number followed by "d". Example: 7d

### Completing tasks

When you use the Done command (Ctrl-D), the active task is saved and marked done.

If you need to check what's been done or make something un-done, use the Done pane. From there if you open a task, it marks it un-done.

### Working with repeating tasks

When you set up a task as repeating, there is still only one copy of the task, but it shows in many places on your agenda or calendar.

Expand the task with "Show all" then click "Add Pattern". You can add as many patters as you need. Since each pattern can only support one 
time of day, then you will use multiple patterns if you need to do the thing multiple times per day.

Use "low clutter" for the task if it has many frequent repeats and you don't want to see that clutter.

The first occurance of the task does not need to be on one of the repeating patterns; for example it can be scheduled at a different time. This does not change
how it repeats.

When you mark a repeating task as done, it reschedules for the next time based on the repeating pattern. When you reach the last time it is intended to repeat
and mark it done, then it sets it permanently done.

By default repeating tasks will repeat forever (but only show one year at most on the calendar or agenda). If it has an end date, click "only until" and 
enter the last day.

### Other task features

Keyboard shortcuts: press Enter in the title to advance to the date, then Enter again to advance to the notes. Or use Tab to go through each field.

The notes can be formatted with headings and bullets. Use the three format buttons on the left of the notes for that purpose.

Notes can also show hyperlinks to web sites. They only show up as clickable when the task is read-only mode, so use Ctrl-Enter first, then you can click on the
link.

Click "Show All" to see all task features. Any feature in use will be shown anyway.

Folder and File fields are used to store Windows paths, such as c:\path\to\a\file. Use the folder in conjuction with a task representing a project,
when you have files stored for that project. This gives you shortcut keys to open the folder in Windows. Use the file entry to store a specific file
name, and then shortcut keys will open the file. (Check the menu for the shortcut keys.)

You can also have it create a file for you in the project folder from a template. For example if you frequently have projects that need a Word file with
some default formatting already set up, then you can store that file as a template, and use shortcut keys to copy that file into the project folder, give it
a name, and open it. Templates are stored in a folder "templates" under the application install folder - you have to manually create that folder and its contents
in order to use the feature.

The password field is used to store a password that is not displayed by default (in case you're in a room with people looking over your shoulder). A quick
copy button lets you copy the un-shown password to the clipboard where you can then paste it into a web site.

The email field is used to store the contents of an email. Suppose you receive an email that you need to deal with on another day. In a perfect world,
Systematizer would do email to and you could just rescheule the email. But we're not there yet, so you need to copy the raw contents of the email (not the 
formatted version) into Systematizer. To do this using Thunderbird email, use keys Ctrl-UAC to copy the raw contents to the copboard, then click Capture
in Systematizer. Once the email is captured, you can schedule the task for later. When it is time to deal with that email, use View to cause Thunderbird to
view the email, at which time you can reply to it. Other mail clients have not been tested but they might work in a similar way.

### Notes

Notes and tasks are the same thing, except you choose "unscheduled" for the schedule type. You can convert a task into a note and back.

Notes do not show on the Today, Agenda, or Calendar panes. Instead they show in the Subject pane.

Use the Subject pane to store things you want to remember that are not things that need to be done at any particular time.

To prevent the subject list from getting too long, you can use an outline. For example you can create a note called "projects", then
create sub-notes under project for each project, and then sub-projects under that. Or if you are storing passwords for web accounts,
create a note called "accounts" and store your account passwords and other information under that.

It's a bit tedious to link up a note as a child of another note after the fact so it is easier to create the child note using the "new sub-item"
command (Ctrl-B).

Often you need to find an account or some other note and don't want to expand the subject tree to find it. In those cases, use the search
feature (F2) to quickly locate it by a keyword.

In some cases you might want the subject outline to contain tasks that are scheduled. You can have a task act like a note by either creating a sub-item
of an existing note, then scheduling it by changing its schedule type, OR you can take any currently scheduled item and make it a child of an existing
note using the linking features (below).

Systematizer is optimized for storing many thousands of notes in the outline.

### People

You can store people and link them to tasks. Here are the main features:

* Person records have the name, notes, phone, email and address fields by default. You can define up to 5 additional fields in the Settings utllity.

* The notes work the same as for tasks.

* Use the command "new linked person" (Ctrl-K) to create a person linked to the active task. Or if you already have the person created and want to create a linked
task, use "new sub-item" (Ctrl-B). Once linked, the next time you open the task or person, you get a quick link to open the person or task respectively.

### People categories

People have categories in an outline format. Use categories for things like where you met them (listing places you've lived), whether they are friends,
family or work contacts, and if they are contractors, stores or other vendors (with as many categories as you like). 

You create categories only while editing a person: click Categories, then click Add Category.

You can manage categories with the Manage categories command (Ctrl-F8). In that utility, you can change people in bulk if you need to adjust
your categorization system.

Categories automatically denote all sub-categories when searching. Notice that when choosing categories, the check mark is only available on the
most nested level. So if you have a category "Vendors" and a subcategory "Mechanics", you cannot choose "Vendors" as the category of a person. But you can
still search on Vendors and the search results will include Mechanics.

### Linking

Tasks can be linked to each other as a parent/child relationship. People can be linked to each other as associated (members of a family
for example). People and tasks can also be linked together.

Linking using the "new sub-item" command is the easiest way, but you can also manage links after you've created tasks and people.

Use the "manage links" command (Ctrl-6, which you can remember because the 6 key has the caret symbol which looks like an arrow) to open
a pane where it gives instructions for linking and unlinking. Activate the desired target pane to cause the link pane to show the linking
option for that target.

### Import/export

The import/export utility can be used to copy data to/from some other calendar app. If you are importing into a blank file, first create a sample record
and export it, so you can see the format. Then make your importable data match that format exactly.

Warning: Imports can be dangerous. If you import the same file twice accidentally, there is no way to undo it.

The "export as HTML" feature is useful if you have secondary device (a phone) but your master copy is on a desktop computer, but you want access to 
some of the Systematizer content while you are away. If you export to HTML then copy the HTML file to the phone, you can crudely use the browser
search feature to find things in the file.

