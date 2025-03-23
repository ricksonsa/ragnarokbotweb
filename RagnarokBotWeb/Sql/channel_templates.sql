
INSERT INTO "ChannelTemplates"("Name", "CategoryName", "ChannelType", "Admin", "CreateDate")
VALUES ('Chat', '⚞⚌⚌ Ragnarok Bot ⚌⚌⚟', 'chat', false, CURRENT_DATE),
        ('No_Admin_Abuser', '⚞⚌⚌ Ragnarok Bot ⚌⚌⚟', 'no-admin-abuse-public', false, CURRENT_DATE),
        ('Kill_Feed', '⚞⚌⚌ Ragnarok Bot ⚌⚌⚟', 'kill-feed', false, CURRENT_DATE),
        ('In_Game_Chat', '⚞⚌⚌ Ragnarok Bot ⚌⚌⚟', 'game-chat', false, CURRENT_DATE),
        ('Welcome_Pack', '⚞⚌⚌ Shop ⚌⚌⚟', 'register', false, CURRENT_DATE),
        ('Taxi', '⚞⚌⚌ Shop ⚌⚌⚟', 'taxi', false, CURRENT_DATE),
        ('Kill_Rank', '⚞⚌⚌ RANKS ⚌⚌⚟', 'kill-rank', false, CURRENT_DATE),
        ('Sniper_Rank', '⚞⚌⚌ RANKS ⚌⚌⚟', 'sniper-rank', false, CURRENT_DATE),
        ('Top_Killer_Day', '⚞⚌⚌ RANKS ⚌⚌⚟', 'top-killer-rank', false, CURRENT_DATE),
        ('Lockpick_Rank', '⚞⚌⚌ RANKS ⚌⚌⚟', 'lockpick-rank', false, CURRENT_DATE),
        ('Top_Lockpick_Day', '⚞⚌⚌ RANKS ⚌⚌⚟', 'top-lockpick-rank', false, CURRENT_DATE),
        ('Bunker_Activation', '⚞⚌⚌ Ragnarok Bot ⚌⚌⚟', 'bunker-states', false, CURRENT_DATE),
        ('No_Admin_Abuser_Private', '⚞⚌⚌ Admin ⚌⚌⚟', 'no-admin-abuse-private', true, CURRENT_DATE),
        ('Admin_Alert', '⚞⚌⚌ Admin ⚌⚌⚟', 'admin-alert', true, CURRENT_DATE),
        ('Login', '⚞⚌⚌ Admin ⚌⚌⚟', 'login', true, CURRENT_DATE),
        ('Buried_Chest', '⚞⚌⚌ Admin ⚌⚌⚟', 'buried-chest', true, CURRENT_DATE),
        ('Mine_Kill', '⚞⚌⚌ Admin ⚌⚌⚟', 'mine-kill', true, CURRENT_DATE),
        ('Lockpick_Alert', '⚞⚌⚌ Admin ⚌⚌⚟', 'lockpick-alert', true, CURRENT_DATE),
        ('Admin_Kill', '⚞⚌⚌ Admin ⚌⚌⚟', 'admin-kill', true, CURRENT_DATE);
       
INSERT INTO "ButtonTemplates"("Name", "Command", "Public", "ChannelTemplateId", "CreateDate")
VALUES ('Welcome Pack', '!welcome_pack', true, 
        (SELECT "Id" FROM "ChannelTemplates" WHERE "ChannelType" = 'register' LIMIT 1), 
        CURRENT_DATE);