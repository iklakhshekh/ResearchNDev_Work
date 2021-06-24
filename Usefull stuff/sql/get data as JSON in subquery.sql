select msid, 
(select '[' + STUFF((
        select 
            ',{"role_name":' + '"'+cast(role.role_name as varchar(max))+'"'
            +'}'
        from user_role_map urole 
		inner join user_role role on role.user_role_pk = urole.user_role_fk
		where urole.userdetail_fk= u.userdetail_pk
        for xml path(''), type
    ).value('.', 'varchar(max)'), 1, 1, '') + ']') Role

	from userdetail u