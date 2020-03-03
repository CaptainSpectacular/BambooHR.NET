Add-PSSnapin Microsoft.Exchange.Management.PowerShell.SnapIn
Import-Module ActiveDirectory

class PasswordGenerator
{
    [string[]] $adverbs
    [string[]] $nouns

    PasswordGenerator()
    {
        $this.adverbs = @("Tango", "Fancy", "Shady", "Scuba", "Ionic", "Jamba")
        $this.nouns   = @("Goose", "Remix", "Yacht", "Plane", "Mango", "Zebra")
    }

    [string] GeneratePassword()
    {
        $num = Get-Random -Maximum 6 -Minimum 0
        $num2 = Get-Random -Maximum 6 -Minimum 0
        $num3 = Get-Random -Maximum 10 -Minimum 0
        return "$($this.adverbs[$num])$($this.nouns[$num2])$($num3)$($num2)"
    }
}

class Mailer
{
    Mailer()
    {}

    [void] SendEmail([string] $sam, [PSCustomObject] $user, [string] $plainPassword)
    {
        # You can import a file using Get-Content, 
        # but then you have to mess around with the 
        # $ExecutionContext, which doesn't work well in between classes
        $mailBody = "Hello,`n`nPlease find $($user.Firstname) $($user.LastName)'s credentials below:`n`nUsername: $($sam)`nEmail: $($user.AnticipatedEmail)`nPassword: $($plainPassword)`n`nPlease do not respond to this email and reach out to your Greystone Engineer if you have any questions.`n`nThank you,`n`nBambooBot"

        Send-MailMessage -To "someaccount@somedomain.com" `
            -From       "someaccount@somedomain.com" `
            -Subject    "New Hire Credentials: $($user.FirstName) $($user.LastName)" `
            -Body       $mailBody `
            -SmtpServer "some.server.com" `
            -Port       "25" `
            -UseSsl
    }
}

class BambooAgent
{
    [PasswordGenerator] $passwordGenerator
    [Mailer] $mailer

    BambooAgent()
    {
        $this.passwordGenerator = [PasswordGenerator]::new()
        $this.mailer = [Mailer]::new()
    }

    [bool] Create([PSCustomObject] $user)
    {
        $defaultGroups = @("Default Group1",
                           "Default Group2")

        try
        {
            $plainPassword = $this.passwordGenerator.GeneratePassword()
            $password = ConvertTo-SecureString -AsPlainText $plainPassword -Force
            $sam = $this.FindValidSAM($user)
            New-ADUser -AccountPassword $password `
                -ChangePasswordAtLogon $false `
                -Enabled $true `
                -Name "$($user.FirstName) $($user.LastName)" `
                -SamAccountName $sam `
                -Path $this.GetOU($user) `
                -UserPrincipalName "$($sam)@somedomain.com"

            $properties = @{}

            if ($user.Company) { $properties.Add("Company", $user.Company) }
            if ($user.Department) { $properties.Add("Department", $user.Department) }
            if ($user.DisplayName) { $properties.Add("DisplayName", $user.DisplayName) }
            if ($user.AnticipatedEmail) { $properties.Add("EmailAddress", $user.AnticipatedEmail) }
            if ($user.FirstName) { $properties.Add("GivenName", $user.FirstName) }
            if ($user.AnticipatedManagerSAM) { $properties.Add("Manager", $this.FindAccurateManager($user)) }
            if ($user.Office) { $properties.Add("Office", $user.Office) }
            if ($user.LastName) { $properties.Add("Surname", $user.LastName) }
            if ($user.WorkPhone) { $properties.Add("OfficePhone", $user.WorkPhone) }
            if ($user.MobilePhone) { $properties.Add("MobilePhone", $user.MobilePhone) }
            if ($user.State) { $properties.Add("State", $user.State) }
            if ($user.JobTitle) { $properties.Add("Title", $user.JobTitle) }

            Set-ADUser $sam @properties
            Set-ADUser $sam -Description "Created $(Get-Date -Format "MM-dd-yyyy") :: BambooBot"
            Set-ADUser $sam -Add @{ proxyAddresses = "SMTP:$($user.AnticipatedEmail)" }

            Enable-MailUser $user.AnticipatedEmail -ExternalEmailAddress $user.AnticipatedEmail
            Enable-RemoteMailbox $user.AnticipatedEmail

            $this.mailer.SendEmail($sam, $user, $plainPassword)

            foreach ($group in $defaultGroups)
            {
                Add-ADGroupMember -Identity $group -Members $sam
            }

            return $true
        }
        catch
        {
            return $false
        }
    }

    [bool] Update([PSCustomObject] $user)
    {
        try
        {
            $sam = $user.CustomLoginName
            $properties = @{}

            if ($user.Company) { $properties.Add("Company", $user.Company) }
            if ($user.Department) { $properties.Add("Department", $user.Department) }
            if ($user.DisplayName) { $properties.Add("DisplayName", $user.DisplayName) }
            if ($user.WorkEmail) { $properties.Add("EmailAddress", $user.WorkEmail) }
            if ($user.FirstName) { $properties.Add("GivenName", $user.FirstName) }
            if ($user.AnticipatedManagerSAM) { $properties.Add("Manager", $this.FindAccurateManager($user)) }
            if ($user.Office) { $properties.Add("Office", $user.Office) }
            if ($user.LastName) { $properties.Add("Surname", $user.LastName) }
            if ($user.WorkPhone) { $properties.Add("OfficePhone", $user.WorkPhone) }
            if ($user.MobilePhone) { $properties.Add("MobilePhone", $user.MobilePhone) }
            if ($user.State) { $properties.Add("State", $user.State) }
            if ($user.JobTitle) { $properties.Add("Title", $user.JobTitle) }
            if ($user.WorkEmail -ne (Get-ADUser $sam).EmailAddress -and $user.WorkEmail)
            { 
                Set-ADUser $sam -Add @{ proxyAddresses = "smtp:$($user.WorkEmail)" } 
            }

            Set-ADUser $sam @properties
            Get-ADUser $sam | Move-ADObject -TargetPath $this.GetOU($user)
        }
        catch
        {
            return $false
        }

        return $true
    }

    [bool] Terminate([PSCustomObject] $user)
    {
        try
        {
            $sam = $user.CustomLoginName

            Set-ADAccountPassword $sam -NewPassword (ConvertTo-SecureString -AsPlainText $this.passwordGenerator.GeneratePassword() -Force)
            Disable-ADAccount $sam
            Set-ADUser $sam -Description "Terminated: $(Get-Date -Format "MM-dd-yyyy") :: BambooBot"
            Set-ADUser $sam -Replace @{ msExchHideFromAddressLists = $true }
            Get-ADUser $sam | Move-ADObject -TargetPath "OU=Terminated Accounts,DC=domain, DC=com"
        }
        catch
        {
            return $false    
        }

        return $true
    }

    [string] FindValidSAM([PSCustomObject] $user)
    {
        $name = $user.AnticipatedSAM
        $suffix = 0
        while($true)
        {
            $suffix += 1
            try
            {
                Get-ADUser $name
                $name = $user.AnticipatedSAM + $suffix
            }
            catch
            {
                break
            }
        }

        return $name
    }

    [string] FindAccurateManager([PSCustomObject] $user)
    {
        $supervisor = $user.Supervisor
        $found = Get-ADUser $user.AnticipatedManagerSAM -Properties DisplayName
        $suffix = 0
        while ("$($found.GivenName) $($found.Surname)" -ne $supervisor)
        {
            $suffix += 1

            try
            {
                $found = (Get-ADUser ($user.AnticipatedManagerSAM + $suffix) `
                    -Properties DisplayName)
            }
            catch
            {
                $found = $null
                break
            }
        }

        if ($null -eq $found)
        {
            $fName = $supervisor.Split[0]
            $lName = $supervisor.Split[1]
            $found = (Get-ADUser -Filter { GivenName -eq $fName -and Surname -eq $lName })[0]
        }
        if ($found -eq $null) { return $null }
        return $found.SAMAccountName
    }

    [string] GetOU([PSCustomObject] $user)
    {
        $offices = @{
            GA = "Georgia"
            CO = "Colorado"
            NV = "Nevada"
        }

        $office = $offices[$user.State]
        if ($null -eq $office -or $office -eq "")
        {
            $office = "Remote"
        }

        return "OU=Users,OU=$($office)DC=somedomain,DC=com"
    }

    [bool] UserExists([PSCustomObject] $user)
    {
        try
        {
            Get-ADUser $user.CustomLoginName
        }
        catch 
        {
            return $false
        }

        return $true
    }

    [bool] IsEnabled([PSCustomObject] $user)
    {
        try
        {
            return (Get-ADUser $user.CustomLoginName -Properties Enabled).Enabled
        }
        catch
        {
            return $false
        }
    }
}

###############################################################################
###############################################################################
#####                                MAIN                                ######
###############################################################################
###############################################################################

$agent = [BambooAgent]::new()
$baseFolder = "\\Some\Shared\File\path"
$bambooChanges = Import-Csv "$($baseFolder)\BambooEmployeeUpdates.csv"
$date = Get-Date -Format "MM-dd-yyyy"
$time = Get-Date -Format "MM-dd-yyyy-hhmmtt"

if (!(Test-Path "$($baseFolder)\$($date)"))
{
    New-Item -Path $baseFolder -Name $date -Type Directory
}

if ($bambooChanges.Count -gt -1) # If there is one change -gt 0 will be false
{
    foreach($user in $bambooChanges)
    {
        [bool] $exists = $agent.UserExists($user)
        [bool] $enabled = $agent.IsEnabled($user)
    
        if (!$exists -and $user.Status -eq "Active")
        {
            [bool] $created = $agent.Create($user)
    
            if ($created)
            {
                $user | Export-Csv -Path "$($baseFolder)\$($date)\SuccessfulCreations$($time).csv" -Append
            }
            else
            {
                $user | Export-Csv -Path "$($baseFolder)\$($date)\FailedCreations$($time).csv" -Append
            }
    
            Remove-Variable -Name "created"
        }
        elseif (!$exists) { }
        elseif ($user.Status -eq "Active" -and $enabled)
        {
            [bool] $updated = $agent.Update($user)
    
            if ($updated)
            {
                $user | Export-Csv -Path "$($baseFolder)\$($date)\SuccessfulUpdates$($time).csv" -Append
            }
            else
            {
                $user | Export-Csv -Path "$($baseFolder)\$($date)\FailedUpdates$($time).csv" -Append
            }
            Remove-Variable -Name "updated"
    
        }
        elseif ($user.Status -eq "Inactive" -and $enabled)
        {
            [bool] $terminated = $agent.Terminate($user)
    
            if ($terminated)
            {
                $user | Export-Csv -Path "$($baseFolder)\$($date)\SuccessfulTerminations$($time).csv" -Append
            }
            else
            {
                $user | Export-Csv -Path "$($baseFolder)\$($date)\FailedTerminations$($time).csv" -Append
            }
    
            Remove-Variable -Name "terminated"
        }
    
        Remove-Variable -Name "user"
        Remove-Variable -Name "exists"
        Remove-Variable -Name "enabled"
    }
    
    Remove-ChildItem "$($baseFolder)\BambooEmployeeUpdates.csv"    
}
