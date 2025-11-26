pipeline {
    agent any

    environment {
        DOCKERHUB_USERNAME = credentials('DOCKERHUB_USERNAME')
        DOCKERHUB_TOKEN = credentials('DOCKERHUB_TOKEN')
        STAGING_HOST = credentials('STAGING_HOST')
        STAGING_USERNAME = credentials('STAGING_USERNAME')
        STAGING_SSH_KEY = credentials('STAGING_SSH_KEY')
        STAGING_DB_PASSWORD = credentials('STAGING_DB_PASSWORD')
        PROD_HOST = credentials('PROD_HOST')
        PROD_USERNAME = credentials('PROD_USERNAME')
        PROD_SSH_KEY = credentials('PROD_SSH_KEY')
        PROD_DB_PASSWORD = credentials('PROD_DB_PASSWORD')
        SMTP_SERVER = credentials('SMTP_SERVER')
        SMTP_PORT = credentials('SMTP_PORT')
        SMTP_USERNAME = credentials('SMTP_USERNAME')
        SMTP_PASSWORD = credentials('SMTP_PASSWORD')
        NOTIFY_EMAILS = credentials('NOTIFY_EMAILS')
        MONITORING_TOKEN = credentials('MONITORING_TOKEN')
    }

    triggers {
        // Ejecuta el pipeline automáticamente en cada push al repositorio
        pollSCM('* * * * *')
    }

    stages {
        stage('Build & Test') {
            steps {
                // Descargar el código fuente y preparar entorno .NET
                checkout scm
                sh 'dotnet --version'
                sh 'dotnet restore certiweb-platform.sln'
                sh 'dotnet build certiweb-platform.sln --no-restore'
                // Ejecutar pruebas unitarias y de integración
                sh 'dotnet test certiweb-platform.sln --no-build --verbosity normal'
            }
        }
        stage('Build & Push Docker Image') {
            when {
                branch 'main'
            }
            steps {
                // Construir y publicar la imagen Docker
                sh """
                    echo $DOCKERHUB_TOKEN | docker login -u $DOCKERHUB_USERNAME --password-stdin
                    docker build -f CertiWeb.API/Dockerfile -t svennn/certiweb-api:latest .
                    docker push svennn/certiweb-api:latest
                """
            }
        }
        stage('Deploy to Staging') {
            steps {
                // Desplegar en el servidor de Staging vía SSH
                sh """
                    ssh -i $STAGING_SSH_KEY $STAGING_USERNAME@$STAGING_HOST '
                        cd /var/www/certiweb-back &&
                        docker pull svennn/certiweb-api:latest &&
                        export MYSQL_ROOT_PASSWORD=$STAGING_DB_PASSWORD &&
                        export CONNECTION_STRING="server=mysql;user=root;password=$STAGING_DB_PASSWORD;database=certiweb" &&
                        docker-compose -f CertiWeb.API/docker-compose.yaml up -d --no-build api
                    '
                """
            }
        }
        stage('Deploy to Production') {
            steps {
                // Desplegar en el servidor de Producción vía SSH
                sh """
                    ssh -i $PROD_SSH_KEY $PROD_USERNAME@$PROD_HOST '
                        cd /var/www/certiweb-back &&
                        docker pull svennn/certiweb-api:latest &&
                        export MYSQL_ROOT_PASSWORD=$PROD_DB_PASSWORD &&
                        export CONNECTION_STRING="server=mysql;user=root;password=$PROD_DB_PASSWORD;database=certiweb_prod" &&
                        docker-compose -f CertiWeb.API/docker-compose.yaml up -d --no-build api
                    '
                """
            }
        }
        stage('Monitoring & Alerts') {
            steps {
                // Enviar correo electrónico tras despliegue en producción
                mail to: "${NOTIFY_EMAILS}",
                     subject: "Despliegue completado en producción para CertiWeb",
                     body: "El despliegue en producción para CertiWeb se ha completado exitosamente.\nRevisa monitoreo en http://grafana.certiweb.com",
                     from: "CertiWeb CI/CD <${SMTP_USERNAME}>"
                // Llamar a endpoint de monitoreo externo
                sh """
                    curl -X POST https://monitoring.certiweb.com/api/checks/certiweb-prod?token=$MONITORING_TOKEN
                """
            }
        }
    }
}
