namespace Systematizer.WPF;

class RecordLinkController 
{
    public RecordLinkVM VM { get; set; } = new RecordLinkVM();
    BlockController Source; //block that the link panel was activated for
    BlockController Target; //block that was selected after activation, or null

    public RecordLinkController()
    {
        //hook up VM behaviors
        VM.ActionRequested = itemVM =>
        {
            WriteLink(itemVM.Command);
            VM.IsActive = false;
        };
    }

    public void ActivateFor(ExtBoxController block)
    {
        //cancel if not saved
        if (block.VM.Persistent.Box.RowId == 0)
        {
            UIGlobals.Do.ShowTimedMessge("Save before creating links");
            return;
        }

        Source = block;
        VM.IsActive = true;
        VM.Instructions = "Use this to link/relink/unlink this item. Open another task, note, or person to see all options.";
        VM.Items.Clear();
        var box = block.VM.Persistent;

        //convert ExtBox.Links to VM items
        foreach (var link in box.Links)
        {
            //don't show child boxes - there could be many of them and there is a lot of complexity with the ui, since the child
            //box could be open with unsaved changes
            if (link.Link == LinkType.FromBoxToChildBox) continue;

            var item = new RecordLinkVM.ItemVM
            {
                IsSticky = true,
                ButtonText = "Unlink",
                Command = new LinkInstruction
                {
                    FromId = box.Box.RowId,
                    IsRemove = true,
                    Link = link.Link,
                    ToId = link.OtherId,
                    ToDescription = link.Description
                }
            };
            if (link.Link == LinkType.FromBoxToParentBox)
                item.Description = $"'{box.Box.Title}' is a sub-item of {link.Description}";
            else 
                item.Description = $"'{box.Box.Title}' is linked to {link.Description}";
            VM.Items.Add(item);
        }
    }

    public void ActivateFor(ExtPersonController block)
    {
        //cancel if not saved
        if (block.VM.Persistent.Person.RowId == 0)
        {
            UIGlobals.Do.ShowTimedMessge("Save before creating links");
            return;
        }

        Source = block;
        VM.IsActive = true;
        VM.Instructions = "Use this to link/relink/unlink this person. Open a task, note, or another person to see all options.";
        VM.Items.Clear();
        var ep = block.VM.Persistent;

        //convert ExtPerson.Links to VM items
        foreach (var link in ep.Links)
        {
            var item = new RecordLinkVM.ItemVM()
            {
                IsSticky = true,
                ButtonText = "Unlink",
                Command = new LinkInstruction
                {
                    FromId = ep.Person.RowId,
                    IsRemove = true,
                    Link = link.Link,
                    ToId = link.OtherId,
                    ToDescription = link.Description
                },
                Description = $"'{ep.Person.Name}' is linked to '{link.Description}'"
            };
            VM.Items.Add(item);
        }
    }

    public void BlockActivated(BlockController bc)
    {
        //NOTE - early returns happen below if the target block is unsaved; then Target is not set
        Target = null;

        //remove VM items that are dependent on the active block
        for (int i = VM.Items.Count - 1; i >= 0; --i)
            if (!VM.Items[i].IsSticky)
                VM.Items.RemoveAt(i);

        //BEGIN TARGET=BOX
        if (bc is ExtBoxController targetBlock1)
        {
            var targetBox = targetBlock1.VM.Persistent.Box;
            if (targetBox.RowId == 0) return;
            if (Source is ExtBoxController sourceBlock1)
            {
                var sourceBox = sourceBlock1.VM.Persistent;

                //option to link source box to parent box
                if (sourceBox.Box.RowId != targetBox.RowId && targetBox.RowId != 0)
                {
                    VM.Items.Add(new RecordLinkVM.ItemVM
                    {
                        ButtonText = "Link",
                        Description = $"Make '{sourceBox.Box.Title}' a sub-item of '{targetBox.Title}'", 
                        Command = new LinkInstruction
                        {
                            FromId = sourceBox.Box.RowId,
                            Link = LinkType.FromBoxToParentBox,
                            ToId = targetBox.RowId,
                            ToDescription = targetBox.Title
                        }
                    });
                }
            }
            else if (Source is ExtPersonController sourceBlock2)
            {
                var sourcePerson = sourceBlock2.VM.Persistent;

                //option to link source person to target box
                VM.Items.Add(new RecordLinkVM.ItemVM
                {
                    ButtonText = "Link",
                    Description = $"Associate '{targetBox.Title}' with '{sourcePerson.Person.Name}'",
                    Command = new LinkInstruction
                    {
                        FromId = sourcePerson.Person.RowId,
                        Link = LinkType.FromPersonToBox,
                        ToId = targetBox.RowId,
                        ToDescription = targetBox.Title
                    }
                });
            }
        }
        //END TARGET=BOX

        //BEGIN TARGET=PERSON
        else if(bc is ExtPersonController targetBlock2)
        {
            var targetPerson = targetBlock2.VM.Persistent.Person;
            if (targetPerson.RowId == 0) return;
            if (targetBlock2 == Source) return;
            if (Source is ExtBoxController sourceBlock1)
            {
                var sourceBox = sourceBlock1.VM.Persistent;

                //option to link source box to target person
                VM.Items.Add(new RecordLinkVM.ItemVM
                {
                    ButtonText = "Link",
                    Description = $"Associate '{sourceBox.Box.Title}' with '{targetPerson.Name}'",
                    Command = new LinkInstruction
                    {
                        FromId = sourceBox.Box.RowId,
                        Link = LinkType.FromBoxToPerson,
                        ToId = targetPerson.RowId,
                        ToDescription = targetPerson.Name
                    }
                });
            }
            else if (Source is ExtPersonController sourceBlock2)
            {
                var sourcePerson = sourceBlock2.VM.Persistent;

                //option to link source person to target person
                VM.Items.Add(new RecordLinkVM.ItemVM
                {
                    ButtonText = "Link",
                    Description = $"Associate '{targetPerson.Name}' with '{sourcePerson.Person.Name}'",
                    Command = new LinkInstruction
                    {
                        FromId = sourcePerson.Person.RowId,
                        Link = LinkType.FromPersonToPerson,
                        ToId = targetPerson.RowId,
                        ToDescription = targetPerson.Name
                    }
                });
            }
        }
        //END TARGET=PERSON

        Target = bc;
    }

    void WriteLink(LinkInstruction cmd) 
    {
        //link/unlink parent box: this happens in VM first, then auto-save box
        if (cmd.Link == LinkType.FromBoxToParentBox)
        {
            if (Source is ExtBoxController sourceBlock)
            {
                var sourceVM = sourceBlock.VM;
                if (cmd.IsRemove)
                    sourceVM.ParentId = null;
                else
                    sourceVM.ParentId = cmd.ToId;
                sourceVM.IsDirty = true;
                sourceBlock.ChangeMode(BaseController.Mode.Edit, true);

                sourceBlock.ReloadLinks();
            }
            if (Target is ExtBoxController targetBlock)
            {
                targetBlock.ReloadLinks();
            }
        }

        //box to child box is not handled; there should be no commands of that type
        else if (cmd.Link == LinkType.FromBoxToChildBox)
        {
        }

        else if (cmd.Link == LinkType.FromBoxToPerson)
        {
            UIService.WritePersonLink(cmd);
            if (Source is ExtBoxController sourceBlock)
            {
                sourceBlock.ReloadLinks();
            }
            if (Target is ExtPersonController targetBlock)
            {
                targetBlock.ReloadLinks();
            }
        }

        else if (cmd.Link == LinkType.FromPersonToBox)
        {
            UIService.WritePersonLink(cmd);
            if (Source is ExtPersonController sourceBlock)
            {
                sourceBlock.ReloadLinks();
            }
            if (Target is ExtBoxController targetBlock)
            {
                targetBlock.ReloadLinks();
            }
        }

        else if (cmd.Link == LinkType.FromPersonToPerson)
        {
            UIService.WritePersonLink(cmd);
            if (Source is ExtPersonController sourceBlock && Target is ExtPersonController targetBlock)
            {
                sourceBlock.ReloadLinks();
                targetBlock.ReloadLinks();
            }
        }
    }
}
