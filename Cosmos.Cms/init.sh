# start SSH
sed -i "s/SSH_PORT/$SSH_PORT/g" /etc/ssh/sshd_config
/usr/sbin/sshd

ls /home/code

dotnet Cosmos.Cms.dll
