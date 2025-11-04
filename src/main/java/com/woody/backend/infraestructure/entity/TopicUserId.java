package com.woody.backend.infraestructure.entity;

import java.util.Objects;

import jakarta.persistence.Embeddable;

@Embeddable
public class TopicUserId {
    private long idUser;
    private long idTopic;

    public TopicUserId(){}

    public TopicUserId(long idUser, long idTopic) {
        this.idUser = idUser;
        this.idTopic = idTopic;
    }

    public long getIdUser() {
        return idUser;
    }

    public void setIdUser(long idUser) {
        this.idUser = idUser;
    }

    public long getIdTopic() {
        return idTopic;
    }

    public void setIdTopic(long idTopic) {
        this.idTopic = idTopic;
    }

    @Override
    public boolean equals(Object o){
        if (this == o) return true;
        if (!(o instanceof TopicUserId that)) return false;
        return Objects.equals(idTopic, that.idTopic) && Objects.equals(idUser, that.idUser);
    }

    @Override
    public int hashCode() {
        return Objects.hash(idTopic, idUser);
    }
}
