use QStatsRLS 
go
	SELECT     Phone.PhoneNumber, COUNT(*) AS EmailCnt	FROM        PhoneEmailXRef INNER JOIN					  Phone ON PhoneEmailXRef.PhoneID = Phone.ID	GROUP BY Phone.PhoneNumber	ORDER BY EmailCnt DESC
	SELECT     Phone.PhoneNumber, COUNT(*) AS AgencyCnt	FROM        PhoneAgencyXRef INNER JOIN					  Phone ON PhoneAgencyXRef.PhoneID = Phone.ID	GROUP BY Phone.PhoneNumber	ORDER BY AgencyCnt DESC

	SELECT     EmailID, COUNT(*) AS PhoneCnt	FROM        PhoneEmailXRef	GROUP BY EmailID	ORDER BY PhoneCnt DESC
	SELECT     AgencyID, COUNT(*) AS PhoneCnt	FROM        PhoneAgencyXRef	GROUP BY AgencyID	ORDER BY PhoneCnt DESC

	SELECT	 Phone.PhoneNumber as [Count>XX], PhoneEmailXRef.EmailID
	FROM	 PhoneEmailXRef INNER JOIN Phone ON PhoneEmailXRef.PhoneID = Phone.ID
	WHERE	 PhoneEmailXRef.EmailID IN (SELECT EmailID FROM PhoneEmailXRef GROUP BY EmailID HAVING (COUNT(*) > 60))
	ORDER BY Phone.PhoneNumber