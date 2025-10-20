FROM openjdk:21-jdk-slim-buster

WORKDIR /app

COPY target/backend-0.0.1-SNAPSHOT.jar /app/

CMD ["java", "-jar", "backend-0.0.1-SNAPSHOT.jar"]